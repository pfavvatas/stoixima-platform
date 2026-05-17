using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Messaging;

public sealed class KafkaConsumerFactory
{
    private readonly KafkaOptions _options;

    public KafkaConsumerFactory(IOptions<KafkaOptions> options)
    {
        _options = options.Value;
    }

    public IConsumer<string, string> Create(string? groupIdOverride = null)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers  = _options.BootstrapServers,
            GroupId           = groupIdOverride ?? _options.GroupId
                                ?? throw new InvalidOperationException("Kafka GroupId is required."),
            AutoOffsetReset   = AutoOffsetReset.Earliest,
            EnableAutoCommit  = false,
            SessionTimeoutMs  = _options.SessionTimeoutMs,
        };

        return new ConsumerBuilder<string, string>(config).Build();
    }

    public static T Deserialize<T>(ConsumeResult<string, string> result)
        => JsonSerializer.Deserialize<T>(result.Message.Value)
           ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}");
}
