using System.Diagnostics;
using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
using Contracts.Events;
using Prometheus;

namespace StorageService.Services;

public class ClickHouseWriter
{
    private readonly string _connectionString;
    private readonly ILogger<ClickHouseWriter> _logger;

    private static readonly Histogram InsertDuration = Metrics.CreateHistogram(
        "storage_clickhouse_insert_duration_ms",
        "Duration of ClickHouse batch insert in milliseconds.",
        new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(10, 2, 10) });

    private static readonly Counter Persisted = Metrics.CreateCounter(
        "storage_messages_persisted_total",
        "Total messages successfully persisted to ClickHouse.");

    private static readonly string[] Columns =
    [
        "timestamp", "message_id", "chat_id", "sender_id",
        "message_text", "has_media", "media_type", "media_file_id",
        "ocr_text", "source", "account_id"
    ];

    public ClickHouseWriter(IConfiguration configuration, ILogger<ClickHouseWriter> logger)
    {
        _connectionString = configuration.GetConnectionString("ClickHouse")
            ?? throw new InvalidOperationException("Missing ClickHouse connection string.");
        _logger = logger;
    }

    public async Task InsertAsync(IReadOnlyList<RawMessageEvent> batch, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        using var bulkCopy = new ClickHouseBulkCopy(_connectionString)
        {
            DestinationTableName = "stoixima.raw_messages",
            ColumnNames          = Columns,
            BatchSize            = batch.Count
        };

        await bulkCopy.InitAsync();

        var rows = batch.Select(m => new object[]
        {
            m.Timestamp.UtcDateTime,
            m.MessageId,
            m.ChatId,
            m.SenderId,
            m.MessageText,
            (byte)(m.HasMedia ? 1 : 0),
            m.MediaType,
            m.MediaFileId,
            string.Empty,   // ocr_text — populated by future OCR step
            m.Source,
            m.AccountId
        });

        await bulkCopy.WriteToServerAsync(rows, ct);

        sw.Stop();
        InsertDuration.Observe(sw.ElapsedMilliseconds);
        Persisted.Inc(batch.Count);

        _logger.LogInformation("ClickHouse: {Count} rows in {Ms}ms", batch.Count, sw.ElapsedMilliseconds);
    }
}
