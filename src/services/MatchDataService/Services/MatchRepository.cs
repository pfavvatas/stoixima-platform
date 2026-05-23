using MatchDataService.Models;
using Npgsql;
using StackExchange.Redis;

namespace MatchDataService.Services;

public class MatchRepository
{
    private readonly string _connectionString;
    private readonly IConnectionMultiplexer _redis;
    private readonly TeamNameNormalizer _normalizer;
    private readonly ILogger<MatchRepository> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(4);

    private const string UpsertMatchSql = """
        INSERT INTO matches (external_id, home_team_id, away_team_id, kick_off, league, season, status, home_score, away_score, updated_at)
        VALUES (@ext_id, @home_id, @away_id, @kick_off, @league, @season, @status, @home_score, @away_score, NOW())
        ON CONFLICT (external_id, league) WHERE external_id IS NOT NULL
        DO UPDATE SET
            status     = EXCLUDED.status,
            home_score = EXCLUDED.home_score,
            away_score = EXCLUDED.away_score,
            updated_at = NOW()
        RETURNING id
        """;

    public MatchRepository(
        IConfiguration configuration,
        IConnectionMultiplexer redis,
        TeamNameNormalizer normalizer,
        ILogger<MatchRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing Postgres connection string.");
        _redis      = redis;
        _normalizer = normalizer;
        _logger     = logger;
    }

    public async Task UpsertAsync(IReadOnlyList<ApiMatch> matches, string league, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var matchIds = new List<int>();

        foreach (var m in matches)
        {
            var homeId = await _normalizer.GetOrCreateTeamIdAsync(conn, m.HomeTeam.Name, league, ct);
            var awayId = await _normalizer.GetOrCreateTeamIdAsync(conn, m.AwayTeam.Name, league, ct);

            var status = NormalizeStatus(m.Status);

            await using var cmd = new NpgsqlCommand(UpsertMatchSql, conn);
            cmd.Parameters.AddWithValue("@ext_id",     m.Id.ToString());
            cmd.Parameters.AddWithValue("@home_id",    homeId);
            cmd.Parameters.AddWithValue("@away_id",    awayId);
            cmd.Parameters.AddWithValue("@kick_off",   m.UtcDate);
            cmd.Parameters.AddWithValue("@league",     league);
            cmd.Parameters.AddWithValue("@season",     (object?)(m.Season?.StartDate) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status",     status);
            cmd.Parameters.AddWithValue("@home_score", (object?)m.Score?.FullTime?.Home ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@away_score", (object?)m.Score?.FullTime?.Away ?? DBNull.Value);

            var matchId = (int)(await cmd.ExecuteScalarAsync(ct) ?? 0);
            if (matchId > 0) matchIds.Add(matchId);
        }

        _logger.LogInformation("Upserted {Count} matches for league {League}", matches.Count, league);

        // Cache in Redis: store today's match IDs under a per-date key
        if (matchIds.Count > 0)
        {
            var db  = _redis.GetDatabase();
            var key = $"matches:today:{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}";
            var values = matchIds.Select(id => (RedisValue)id).ToArray();
            await db.SetAddAsync(key, values);
            await db.KeyExpireAsync(key, CacheTtl);
        }
    }

    private static string NormalizeStatus(string apiStatus) => apiStatus.ToUpperInvariant() switch
    {
        "SCHEDULED" or "TIMED" => "scheduled",
        "IN_PLAY"   or "PAUSED" => "live",
        "FINISHED"  => "finished",
        "POSTPONED" => "postponed",
        _           => "scheduled"
    };
}
