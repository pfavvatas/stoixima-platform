using ClickHouse.Client.Copy;
using Contracts.Events;
using Npgsql;

namespace MessageProcessing.Services;

public class TipPersister
{
    private readonly string _postgresCs;
    private readonly string _clickHouseCs;
    private readonly ILogger<TipPersister> _logger;

    private const string InsertTipSql = """
        INSERT INTO tips (id, channel_id, match_id, raw_message_id, tip_type, tip_value, odds, confidence, source)
        VALUES (@id, @channel, @match, @raw_msg, @tip_type, @tip_value, @odds, @confidence, 'telegram')
        ON CONFLICT (channel_id, match_id, tip_type, tip_value) WHERE is_valid = true DO NOTHING
        """;

    private static readonly string[] ClickHouseColumns =
    [
        "timestamp", "tip_id", "channel_id", "channel_title",
        "match_id", "home_team", "away_team", "league", "kick_off",
        "tip_type", "tip_value", "odds", "confidence", "source", "raw_message_id"
    ];

    public TipPersister(IConfiguration configuration, ILogger<TipPersister> logger)
    {
        _postgresCs   = configuration.GetConnectionString("Postgres")
                        ?? throw new InvalidOperationException("Missing Postgres connection string.");
        _clickHouseCs = configuration.GetConnectionString("ClickHouse")
                        ?? throw new InvalidOperationException("Missing ClickHouse connection string.");
        _logger = logger;
    }

    public async Task PersistAsync(ProcessedTipEvent tip, CancellationToken ct)
    {
        await PersistPostgresAsync(tip, ct);
        await PersistClickHouseAsync(tip, ct);
    }

    private async Task PersistPostgresAsync(ProcessedTipEvent tip, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_postgresCs);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(InsertTipSql, conn);
        cmd.Parameters.AddWithValue("@id",         tip.TipId);
        cmd.Parameters.AddWithValue("@channel",    tip.ChannelId);
        cmd.Parameters.AddWithValue("@match",      tip.MatchId);
        cmd.Parameters.AddWithValue("@raw_msg",    (object?)tip.RawMessageId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@tip_type",   tip.TipType);
        cmd.Parameters.AddWithValue("@tip_value",  tip.TipValue);
        cmd.Parameters.AddWithValue("@odds",       (object?)tip.Odds ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@confidence", tip.Confidence);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task PersistClickHouseAsync(ProcessedTipEvent tip, CancellationToken ct)
    {
        try
        {
            using var bulkCopy = new ClickHouseBulkCopy(_clickHouseCs)
            {
                DestinationTableName = "stoixima.processed_tips",
                ColumnNames          = ClickHouseColumns,
                BatchSize            = 1
            };
            await bulkCopy.InitAsync();

            var row = new object[]
            {
                tip.Timestamp.UtcDateTime,
                tip.TipId,
                tip.ChannelId,
                tip.ChannelTitle,
                tip.MatchId,
                tip.HomeTeam,
                tip.AwayTeam,
                tip.League,
                tip.KickOff.UtcDateTime,
                tip.TipType,
                tip.TipValue,
                (float)(tip.Odds ?? 0m),
                (float)tip.Confidence,
                tip.Source,
                tip.RawMessageId
            };

            await bulkCopy.WriteToServerAsync([row], ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ClickHouse insert failed for tip {TipId} — Postgres record kept", tip.TipId);
        }
    }
}
