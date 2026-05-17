using Contracts.Events;
using Npgsql;

namespace StorageService.Services;

public class PostgresWriter
{
    private readonly string _connectionString;
    private readonly ILogger<PostgresWriter> _logger;

    private const string UpsertChannelSql = """
        INSERT INTO channels (id, title, source, active)
        VALUES (@id, @title, 'telegram', true)
        ON CONFLICT (id) DO UPDATE
            SET title      = EXCLUDED.title,
                updated_at = NOW()
        """;

    public PostgresWriter(IConfiguration configuration, ILogger<PostgresWriter> logger)
    {
        _connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing Postgres connection string.");
        _logger = logger;
    }

    // UPSERTs distinct channels found in the batch.
    public async Task UpsertChannelsAsync(IReadOnlyList<RawMessageEvent> batch, CancellationToken ct)
    {
        var distinct = batch
            .GroupBy(e => e.ChatId)
            .Select(g => g.First())
            .ToList();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        foreach (var evt in distinct)
        {
            await using var cmd = new NpgsqlCommand(UpsertChannelSql, conn);
            cmd.Parameters.AddWithValue("@id",    evt.ChatId);
            cmd.Parameters.AddWithValue("@title", evt.ChatTitle);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        _logger.LogDebug("Postgres: upserted {Count} channel(s)", distinct.Count);
    }
}
