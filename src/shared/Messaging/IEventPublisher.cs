namespace Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(string topic, string partitionKey, T @event, CancellationToken ct = default);
}
