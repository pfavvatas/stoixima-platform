using Confluent.Kafka;
using Contracts;
using Contracts.Events;
using Messaging;
using Prometheus;
using StorageService.Services;

namespace StorageService.Workers;

public class StorageWorker : BackgroundService
{
    private readonly KafkaConsumerFactory _consumerFactory;
    private readonly RedisDeduplicator _deduplicator;
    private readonly ClickHouseWriter _clickHouseWriter;
    private readonly PostgresWriter _postgresWriter;
    private readonly ILogger<StorageWorker> _logger;

    private const int BatchSize    = 500;
    private static readonly TimeSpan BatchTimeout = TimeSpan.FromSeconds(5);
    private const string GroupId   = "storage-service-group";

    private static readonly Counter Duplicates = Metrics.CreateCounter(
        "storage_duplicates_skipped_total",
        "Messages skipped because Redis dedup detected them as already persisted.");

    public StorageWorker(
        KafkaConsumerFactory consumerFactory,
        RedisDeduplicator deduplicator,
        ClickHouseWriter clickHouseWriter,
        PostgresWriter postgresWriter,
        ILogger<StorageWorker> logger)
    {
        _consumerFactory  = consumerFactory;
        _deduplicator     = deduplicator;
        _clickHouseWriter = clickHouseWriter;
        _postgresWriter   = postgresWriter;
        _logger           = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // don't block app startup

        using var consumer = _consumerFactory.Create(GroupId);
        consumer.Subscribe(KafkaTopics.RawMessages);
        _logger.LogInformation("StorageWorker subscribed to {Topic}", KafkaTopics.RawMessages);

        var batch = new List<ConsumeResult<string, string>>(BatchSize);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Collect messages until batch is full or timeout elapses.
                var deadline = DateTime.UtcNow.Add(BatchTimeout);

                while (batch.Count < BatchSize)
                {
                    var msLeft = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
                    if (msLeft <= 0) break;
                    if (stoppingToken.IsCancellationRequested) break;

                    var result = consumer.Consume(Math.Min(msLeft, 500));

                    if (result is { IsPartitionEOF: false })
                        batch.Add(result);
                }

                if (batch.Count > 0)
                    await FlushAsync(consumer, batch, stoppingToken);

                batch.Clear();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            // Best-effort flush of what's left before shutdown.
            if (batch.Count > 0)
            {
                try { await FlushAsync(consumer, batch, CancellationToken.None); }
                catch (Exception ex) { _logger.LogError(ex, "Shutdown flush failed"); }
            }
            consumer.Close();
        }
    }

    private async Task FlushAsync(
        IConsumer<string, string> consumer,
        IReadOnlyList<ConsumeResult<string, string>> batch,
        CancellationToken ct)
    {
        // 1. Deserialize
        var events = batch
            .Select(r => KafkaConsumerFactory.Deserialize<RawMessageEvent>(r))
            .ToList();

        // 2. Redis dedup
        var newEvents = await _deduplicator.FilterDuplicatesAsync(events, ct);

        var dupeCount = events.Count - newEvents.Count;
        if (dupeCount > 0)
        {
            Duplicates.Inc(dupeCount);
            _logger.LogDebug("Deduped {Count} duplicate message(s)", dupeCount);
        }

        // 3. Persist new events
        if (newEvents.Count > 0)
        {
            await _clickHouseWriter.InsertAsync(newEvents, ct);
            await _postgresWriter.UpsertChannelsAsync(newEvents, ct);
        }

        // 4. Commit offsets — only after successful persistence.
        consumer.Commit(batch[^1]);

        _logger.LogInformation(
            "Batch flushed: {Total} consumed, {New} new, {Dupe} dupes",
            batch.Count, newEvents.Count, dupeCount);
    }
}
