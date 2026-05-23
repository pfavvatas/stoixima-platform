using Confluent.Kafka;
using Contracts;
using Contracts.Events;
using Messaging;
using MessageProcessing.Services;

namespace MessageProcessing.Workers;

public class ProcessingWorker : BackgroundService
{
    private readonly KafkaConsumerFactory _consumerFactory;
    private readonly MessageProcessor _processor;
    private readonly ILogger<ProcessingWorker> _logger;

    private const string GroupId = "message-processing-group";

    public ProcessingWorker(
        KafkaConsumerFactory consumerFactory,
        MessageProcessor processor,
        ILogger<ProcessingWorker> logger)
    {
        _consumerFactory = consumerFactory;
        _processor       = processor;
        _logger          = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        using var consumer = _consumerFactory.Create(GroupId);
        consumer.Subscribe(KafkaTopics.RawMessages);
        _logger.LogInformation("ProcessingWorker subscribed to {Topic}", KafkaTopics.RawMessages);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                if (result is null or { IsPartitionEOF: true })
                    continue;

                var raw = KafkaConsumerFactory.Deserialize<RawMessageEvent>(result);

                try
                {
                    await _processor.ProcessAsync(raw, imageBytes: null, stoppingToken);
                    consumer.Commit(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process message {MessageId}", raw.MessageId);
                    // Don't commit — message will be re-delivered on restart
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            consumer.Close();
        }
    }
}
