using Npgsql;

namespace TelegramGateway.Services;

public class ChannelRepository
{
    private readonly string _connectionString;

    public ChannelRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing Postgres connection string.");
    }

    // Returns MTProto channel IDs → title for all active Telegram channels.
    // Bot API format (-1001234567890) is stored in DB; MTProto (1234567890) is used for filtering updates.
    public async Task<IReadOnlyDictionary<long, string>> GetMonitoredChannelsAsync(
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(
            "SELECT id, title FROM channels WHERE active = true AND source = 'telegram'", conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var result = new Dictionary<long, string>();
        while (await reader.ReadAsync(ct))
        {
            long botApiId  = reader.GetInt64(0);
            string title   = reader.GetString(1);
            long mtProtoId = Math.Abs(botApiId) - 1_000_000_000_000L;
            if (mtProtoId > 0)
                result[mtProtoId] = title;
        }
        return result;
    }
}
