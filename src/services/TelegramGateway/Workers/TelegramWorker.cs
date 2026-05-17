using TelegramGateway.Health;

namespace TelegramGateway.Workers;

// Placeholder — full implementation in issue #7 (WTelegramClient integration).
// Sets TelegramConnectionHealthCheck.IsConnected once connected.
public class TelegramWorker : BackgroundService
{
    private readonly ILogger<TelegramWorker> _logger;

    public TelegramWorker(ILogger<TelegramWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TelegramWorker starting — WTelegramClient integration pending (issue #7).");

        // Issue #7: connect to Telegram, subscribe to updates, publish to Kafka.
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        TelegramConnectionHealthCheck.IsConnected = false;
        _logger.LogInformation("TelegramWorker stopped.");
        return base.StopAsync(cancellationToken);
    }
}
