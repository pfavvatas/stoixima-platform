using ApiGateway.Models;
using Npgsql;

namespace ApiGateway.Services;

public class AdminRepository
{
    private readonly string _connectionString;

    public AdminRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing Postgres connection string.");
    }

    public async Task<List<AdminChannelDto>> GetAllChannelsAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT c.id, c.title, c.username, c.active,
                   COUNT(t.id)::int AS tip_count,
                   c.created_at
            FROM   channels c
            LEFT JOIN tips t ON t.channel_id = c.id AND t.is_valid = true
            GROUP  BY c.id, c.title, c.username, c.active, c.created_at
            ORDER  BY c.active DESC, tip_count DESC
            """;

        var channels = new List<AdminChannelDto>();
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            channels.Add(new AdminChannelDto
            {
                Id       = reader.GetInt64(0),
                Title    = reader.GetString(1),
                Username = reader.IsDBNull(2) ? null : reader.GetString(2),
                Active   = reader.GetBoolean(3),
                TipCount = reader.GetInt32(4),
                AddedAt  = reader.GetFieldValue<DateTimeOffset>(5),
            });
        }
        return channels;
    }

    public async Task CreateChannelAsync(long chatId, string title, string? username, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO channels (id, title, username, source, active)
            VALUES (@id, @title, @username, 'telegram', true)
            ON CONFLICT (id) DO UPDATE
                SET title      = EXCLUDED.title,
                    username   = COALESCE(EXCLUDED.username, channels.username),
                    updated_at = NOW()
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id",       chatId);
        cmd.Parameters.AddWithValue("@title",    title);
        cmd.Parameters.AddWithValue("@username", (object?)username ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task SetChannelActiveAsync(long channelId, bool active, CancellationToken ct)
    {
        const string sql = """
            UPDATE channels SET active = @active, updated_at = NOW()
            WHERE id = @id
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id",     channelId);
        cmd.Parameters.AddWithValue("@active", active);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteChannelAsync(long channelId, CancellationToken ct)
    {
        const string sql = "DELETE FROM channels WHERE id = @id";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", channelId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<AdminStatsDto> GetStatsAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT
                (SELECT COUNT(*) FROM channels WHERE active = true)                          AS active_channels,
                (SELECT COUNT(*) FROM channels)                                              AS total_channels,
                (SELECT COUNT(*) FROM matches   WHERE DATE(kick_off) = CURRENT_DATE)        AS matches_today,
                (SELECT COUNT(*) FROM tips      WHERE DATE(extracted_at) = CURRENT_DATE
                                                AND is_valid = true)                         AS tips_today,
                (SELECT COUNT(DISTINCT channel_id) FROM tips
                        WHERE DATE(extracted_at) = CURRENT_DATE AND is_valid = true)        AS active_tippers_today
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);

        return new AdminStatsDto
        {
            ActiveChannels      = (int)(long)reader.GetValue(0),
            TotalChannels       = (int)(long)reader.GetValue(1),
            MatchesToday        = (int)(long)reader.GetValue(2),
            TipsToday           = (int)(long)reader.GetValue(3),
            ActiveTippersToday  = (int)(long)reader.GetValue(4)
        };
    }
}

public record AdminStatsDto
{
    public int ActiveChannels     { get; init; }
    public int TotalChannels      { get; init; }
    public int MatchesToday       { get; init; }
    public int TipsToday          { get; init; }
    public int ActiveTippersToday { get; init; }
}
