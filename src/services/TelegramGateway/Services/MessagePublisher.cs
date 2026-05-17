using System.Threading.Channels;
using Contracts;
using Contracts.Events;
using Messaging;
using Microsoft.Extensions.Caching.Memory;
using Prometheus;

namespace TelegramGateway.Services;

// Registered as both singleton (injectable) and IHostedService (runs the drain loop).
public class MessagePublisher : IHostedService
{
    private readonly IEventPublisher _publisher;
    private readonly IMemoryCache _dedupeCache;
    private readonly ILogger<MessagePublisher> _logger;
    private readonly Channel<RawMessageEvent> _deadLetterBuffer;
    private CancellationTokenSource _cts = new();
    private Task _drainTask = Task.CompletedTask;

    private const int DeadLetterCapacity = 1_000;

    private static readonly Counter Published = Metrics.CreateCounter(
        "telegram_messages_published_total",
        "Total RawMessageEvents successfully published to Kafka.");

    private static readonly Counter PublishErrors = Metrics.CreateCounter(
        "telegram_messages_publish_errors_total",
        "Total Kafka publish failures (message moved to dead-letter buffer).");

    private static readonly Counter DeadLetterBuffered = Metrics.CreateCounter(
        "telegram_deadletter_buffered_total",
        "Messages moved to local dead-letter buffer after Kafka failure.");

    private static readonly Counter DeadLetterDropped = Metrics.CreateCounter(
        "telegram_deadletter_dropped_total",
        "Messages dropped when dead-letter buffer is full.");

    private static readonly Counter DuplicatesSkipped = Metrics.CreateCounter(
        "telegram_messages_duplicates_total",
        "Duplicate message IDs skipped (already published within the last 24 h).");

    private static readonly Gauge DeadLetterDepth = Metrics.CreateGauge(
        "telegram_deadletter_depth",
        "Current number of messages waiting in the local dead-letter buffer.");

    public MessagePublisher(
        IEventPublisher publisher,
        IMemoryCache dedupeCache,
        ILogger<MessagePublisher> logger)
    {
        _publisher   = publisher;
        _dedupeCache = dedupeCache;
        _logger      = logger;

        _deadLetterBuffer = Channel.CreateBounded<RawMessageEvent>(
            new BoundedChannelOptions(DeadLetterCapacity) { SingleReader = true });
    }

    // ─── Publish ─────────────────────────────────────────────────────────────

    public async Task PublishAsync(RawMessageEvent evt, CancellationToken ct = default)
    {
        // Deduplicate: same (chatId, messageId) within 24 h → skip.
        var key = $"msg:{evt.ChatId}:{evt.MessageId}";
        if (_dedupeCache.TryGetValue(key, out _))
        {
            DuplicatesSkipped.Inc();
            return;
        }
        _dedupeCache.Set(key, true, TimeSpan.FromHours(24));

        await TryPublishDirectAsync(evt, ct);
    }

    private async Task TryPublishDirectAsync(RawMessageEvent evt, CancellationToken ct)
    {
        try
        {
            await _publisher.PublishAsync(KafkaTopics.RawMessages, evt.ChatId.ToString(), evt, ct);
            Published.Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kafka unavailable — buffering message {MessageId}", evt.MessageId);
            PublishErrors.Inc();

            if (_deadLetterBuffer.Writer.TryWrite(evt))
            {
                DeadLetterBuffered.Inc();
                DeadLetterDepth.Inc();
            }
            else
            {
                _logger.LogWarning("Dead-letter buffer full ({Cap}) — dropping message {MessageId}",
                    DeadLetterCapacity, evt.MessageId);
                DeadLetterDropped.Inc();
            }
        }
    }

    // ─── IHostedService — drain loop ─────────────────────────────────────────

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts       = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _drainTask = DrainDeadLetterAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _deadLetterBuffer.Writer.TryComplete();
        await _cts.CancelAsync();
        await _drainTask.WaitAsync(cancellationToken);
    }

    // Reads from the dead-letter buffer and retries with exponential backoff.
    private async Task DrainDeadLetterAsync(CancellationToken ct)
    {
        await foreach (var evt in _deadLetterBuffer.Reader.ReadAllAsync(ct))
        {
            DeadLetterDepth.Dec();
            var backoff = TimeSpan.FromSeconds(5);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await _publisher.PublishAsync(
                        KafkaTopics.RawMessages, evt.ChatId.ToString(), evt, ct);
                    Published.Inc();
                    _logger.LogInformation(
                        "Dead-letter drained: message {MessageId} from chat {ChatId}",
                        evt.MessageId, evt.ChatId);
                    break;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Dead-letter retry failed for message {MessageId} — waiting {Delay}",
                        evt.MessageId, backoff);
                    await Task.Delay(backoff, ct);
                    backoff = TimeSpan.FromSeconds(Math.Min((int)backoff.TotalSeconds * 2, 60));
                }
            }
        }
    }
}
