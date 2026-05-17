using Microsoft.Extensions.Options;
using Prometheus;
using TelegramGateway.Configuration;
using TelegramGateway.Health;
using TelegramGateway.Services;
using TL;
using WTelegram;

namespace TelegramGateway.Workers;

public class TelegramWorker : BackgroundService
{
    private readonly TelegramOptions _options;
    private readonly ChannelRepository _channelRepo;
    private readonly UpdatesHandler _updatesHandler;
    private readonly ILogger<TelegramWorker> _logger;

    private static readonly Gauge ConnectedGauge = Metrics.CreateGauge(
        "telegram_client_connected",
        "1 if the Telegram MTProto client is connected, 0 otherwise.");

    public TelegramWorker(
        IOptions<TelegramOptions> options,
        ChannelRepository channelRepo,
        UpdatesHandler updatesHandler,
        ILogger<TelegramWorker> logger)
    {
        _options        = options.Value;
        _channelRepo    = channelRepo;
        _updatesHandler = updatesHandler;
        _logger         = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(5);
        const int MaxDelaySeconds = 300;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSessionAsync(stoppingToken);
                delay = TimeSpan.FromSeconds(5); // reset after clean session end
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (RpcException ex) when (ex.Code == 420)
            {
                var waitSeconds = ex.X + 1;
                _logger.LogWarning("Telegram FloodWait — sleeping {Seconds}s", waitSeconds);
                MarkDisconnected();
                await Task.Delay(TimeSpan.FromSeconds(waitSeconds), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telegram session error — reconnecting in {Delay}", delay);
                MarkDisconnected();
                await Task.Delay(delay, stoppingToken);
                delay = TimeSpan.FromSeconds(
                    Math.Min((int)delay.TotalSeconds * 2, MaxDelaySeconds));
            }
        }

        MarkDisconnected();
    }

    private async Task RunSessionAsync(CancellationToken ct)
    {
        var channels = await _channelRepo.GetMonitoredChannelsAsync(ct);
        _updatesHandler.SetChannels(channels);

        _logger.LogInformation("Loaded {Count} monitored channels", channels.Count);

        using var client  = new Client(ConfigProvider);
        await client.LoginUserIfNeeded();
        client.WithUpdateManager(_updatesHandler.HandleAsync);

        TelegramConnectionHealthCheck.IsConnected = true;
        ConnectedGauge.Set(1);
        _logger.LogInformation("Telegram client connected — listening for updates.");

        // Hold here until the host signals shutdown (or an exception propagates).
        await Task.Delay(Timeout.Infinite, ct);
    }

    private string? ConfigProvider(string what) => what switch
    {
        "api_id"           => _options.ApiId.ToString(),
        "api_hash"         => _options.ApiHash,
        "phone_number"     => _options.AccountPhone,
        "session_pathname" => _options.SessionPath,
        "verification_code" => _options.SetupMode ? ReadCode() : null,
        "first_name"       => "User",
        "last_name"        => "Bot",
        _                  => null
    };

    private static string? ReadCode()
    {
        Console.Write("Enter Telegram OTP: ");
        return Console.ReadLine();
    }

    private static void MarkDisconnected()
    {
        TelegramConnectionHealthCheck.IsConnected = false;
        ConnectedGauge.Set(0);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        MarkDisconnected();
        _logger.LogInformation("TelegramWorker stopped.");
        return base.StopAsync(cancellationToken);
    }
}
