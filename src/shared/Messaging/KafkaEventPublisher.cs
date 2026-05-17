using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Messaging;

public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(IOptions<KafkaOptions> options, ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers      = options.Value.BootstrapServers,
            MessageTimeoutMs      = options.Value.MessageTimeoutMs,
            Acks                  = Acks.Leader,
            EnableIdempotence     = true,
            CompressionType       = CompressionType.Snappy,
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(string topic, string partitionKey, T @event, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(@event);
        var message = new Message<string, string> { Key = partitionKey, Value = json };

        try
        {
            var result = await _producer.ProduceAsync(topic, message, ct);
            _logger.LogDebug("Published to {Topic} partition {Partition} offset {Offset}",
                topic, result.Partition.Value, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish to {Topic} with key {Key}", topic, partitionKey);
            throw;
        }
    }

    public void Dispose() => _producer.Dispose();
}
