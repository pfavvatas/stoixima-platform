using Contracts;
using Contracts.Events;
using Messaging;
using Prometheus;

namespace TelegramGateway.Services;

public class MessagePublisher
{
    private readonly IEventPublisher _publisher;
    private readonly ILogger<MessagePublisher> _logger;

    private static readonly Counter Published = Metrics.CreateCounter(
        "telegram_messages_published_total",
        "Total RawMessageEvents published to Kafka.");

    private static readonly Counter PublishErrors = Metrics.CreateCounter(
        "telegram_messages_publish_errors_total",
        "Total Kafka publish failures.");

    public MessagePublisher(IEventPublisher publisher, ILogger<MessagePublisher> logger)
    {
        _publisher = publisher;
        _logger    = logger;
    }

    public async Task PublishAsync(RawMessageEvent evt, CancellationToken ct = default)
    {
        try
        {
            await _publisher.PublishAsync(
                KafkaTopics.RawMessages,
                evt.ChatId.ToString(),
                evt,
                ct);
            Published.Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message {MessageId} from chat {ChatId}",
                evt.MessageId, evt.ChatId);
            PublishErrors.Inc();
            throw;
        }
    }
}
