using System.Text.Json;
using ApiGateway.Models;
using Contracts.Events;
using Npgsql;
using StackExchange.Redis;

namespace ApiGateway.Services;

public class FeedRepository
{
    private readonly string _connectionString;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<FeedRepository> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FeedRepository(
        IConfiguration configuration,
        IConnectionMultiplexer redis,
        ILogger<FeedRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing Postgres connection string.");
        _redis  = redis;
        _logger = logger;
    }

    public async Task<List<AggregatedMatchDto>> GetFeedAsync(
        DateOnly date,
        string? league,
        int minTippers,
        CancellationToken ct)
    {
        // Try Redis first for today's data
        if (date == DateOnly.FromDateTime(DateTime.UtcNow))
        {
            var cached = await TryGetFromCacheAsync(date, league, minTippers, ct);
            if (cached.Count > 0) return cached;
        }

        return await GetFromDbAsync(date, league, minTippers, ct);
    }

    public async Task<AggregatedMatchDto?> GetMatchDetailAsync(int matchId, CancellationToken ct)
    {
        var db  = _redis.GetDatabase();
        var key = $"feed:match:{matchId}";
        var cached = await db.StringGetAsync(key);
        if (cached.HasValue)
        {
            var evt = JsonSerializer.Deserialize<AggregatedMatchEvent>(cached!, JsonOptions);
            if (evt is not null) return MapToDto(evt, "scheduled");
        }

        return await GetMatchFromDbAsync(matchId, ct);
    }

    public async Task<List<MatchDto>> GetMatchesAsync(DateOnly date, CancellationToken ct)
    {
        const string sql = """
            SELECT m.id, th.name, ta.name, m.league, m.kick_off, m.status, m.home_score, m.away_score
            FROM   matches m
            JOIN   teams th ON th.id = m.home_team_id
            JOIN   teams ta ON ta.id = m.away_team_id
            WHERE  DATE(m.kick_off) = @date
            ORDER  BY m.kick_off
            """;

        var matches = new List<MatchDto>();
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@date", date.ToDateTime(TimeOnly.MinValue));
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            matches.Add(new MatchDto
            {
                Id        = reader.GetInt32(0),
                HomeTeam  = reader.GetString(1),
                AwayTeam  = reader.GetString(2),
                League    = reader.GetString(3),
                KickOff   = reader.GetFieldValue<DateTimeOffset>(4),
                Status    = reader.GetString(5),
                HomeScore = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                AwayScore = reader.IsDBNull(7) ? null : reader.GetInt32(7)
            });
        }
        return matches;
    }

    public async Task<List<ChannelDto>> GetChannelsAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT c.id, c.title, c.source, c.active,
                   COUNT(t.id)::int AS tip_count
            FROM   channels c
            LEFT JOIN tips t ON t.channel_id = c.id AND t.is_valid = true
            WHERE  c.active = true
            GROUP  BY c.id, c.title, c.source, c.active
            ORDER  BY tip_count DESC
            """;

        var channels = new List<ChannelDto>();
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd  = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            channels.Add(new ChannelDto
            {
                Id       = reader.GetInt64(0),
                Title    = reader.GetString(1),
                Source   = reader.GetString(2),
                Active   = reader.GetBoolean(3),
                TipCount = reader.GetInt32(4)
            });
        }
        return channels;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<List<AggregatedMatchDto>> TryGetFromCacheAsync(
        DateOnly date, string? league, int minTippers, CancellationToken ct)
    {
        var db  = _redis.GetDatabase();
        var key = $"matches:today:{date:yyyy-MM-dd}";
        var matchIds = await db.SetMembersAsync(key);

        var results = new List<AggregatedMatchDto>();
        foreach (var id in matchIds)
        {
            var feedKey = $"feed:match:{id}";
            var json    = await db.StringGetAsync(feedKey);
            if (!json.HasValue) continue;

            var evt = JsonSerializer.Deserialize<AggregatedMatchEvent>(json!, JsonOptions);
            if (evt is null) continue;
            if (evt.TotalTippers < minTippers) continue;
            if (league is not null && !evt.League.Equals(league, StringComparison.OrdinalIgnoreCase)) continue;

            results.Add(MapToDto(evt, "scheduled"));
        }

        return results.OrderBy(m => m.KickOff).ToList();
    }

    private async Task<List<AggregatedMatchDto>> GetFromDbAsync(
        DateOnly date, string? league, int minTippers, CancellationToken ct)
    {
        var leagueFilter = league is not null ? "AND m.league = @league" : string.Empty;
        var sql = $"""
            SELECT m.id, th.name, ta.name, m.league, m.kick_off, m.status,
                   COUNT(DISTINCT t.channel_id) AS tippers,
                   COALESCE(json_agg(json_build_object(
                       'channelId', t.channel_id,
                       'channelTitle', c.title,
                       'tipType', t.tip_type,
                       'tipValue', t.tip_value,
                       'odds', t.odds
                   )) FILTER (WHERE t.id IS NOT NULL), '[]') AS tips_json
            FROM   matches m
            JOIN   teams th ON th.id = m.home_team_id
            JOIN   teams ta ON ta.id = m.away_team_id
            LEFT JOIN tips t ON t.match_id = m.id AND t.is_valid = true
            LEFT JOIN channels c ON c.id = t.channel_id
            WHERE  DATE(m.kick_off) = @date
            {leagueFilter}
            GROUP  BY m.id, th.name, ta.name, m.league, m.kick_off, m.status
            HAVING COUNT(DISTINCT t.channel_id) >= @min_tippers
            ORDER  BY m.kick_off
            """;

        var results = new List<AggregatedMatchDto>();
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@date",        date.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("@min_tippers", minTippers);
        if (league is not null)
            cmd.Parameters.AddWithValue("@league", league);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var matchId = reader.GetInt32(0);
            var tipsJson = reader.GetString(7);
            var tips = JsonSerializer.Deserialize<List<TipDetailDto>>(tipsJson, JsonOptions) ?? [];

            results.Add(new AggregatedMatchDto
            {
                MatchId      = matchId,
                HomeTeam     = reader.GetString(1),
                AwayTeam     = reader.GetString(2),
                League       = reader.GetString(3),
                KickOff      = reader.GetFieldValue<DateTimeOffset>(4),
                Status       = reader.GetString(5),
                TotalTippers = (int)(long)reader.GetValue(6),
                Tips         = tips,
                Consensus    = BuildConsensus(tips),
                UpdatedAt    = DateTimeOffset.UtcNow
            });
        }
        return results;
    }

    private async Task<AggregatedMatchDto?> GetMatchFromDbAsync(int matchId, CancellationToken ct)
    {
        const string sql = """
            SELECT m.id, th.name, ta.name, m.league, m.kick_off, m.status,
                   COUNT(DISTINCT t.channel_id) AS tippers,
                   COALESCE(json_agg(json_build_object(
                       'channelId', t.channel_id,
                       'channelTitle', c.title,
                       'tipType', t.tip_type,
                       'tipValue', t.tip_value,
                       'odds', t.odds
                   )) FILTER (WHERE t.id IS NOT NULL), '[]') AS tips_json
            FROM   matches m
            JOIN   teams th ON th.id = m.home_team_id
            JOIN   teams ta ON ta.id = m.away_team_id
            LEFT JOIN tips t ON t.match_id = m.id AND t.is_valid = true
            LEFT JOIN channels c ON c.id = t.channel_id
            WHERE  m.id = @match_id
            GROUP  BY m.id, th.name, ta.name, m.league, m.kick_off, m.status
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@match_id", matchId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        var tipsJson = reader.GetString(7);
        var tips = JsonSerializer.Deserialize<List<TipDetailDto>>(tipsJson, JsonOptions) ?? [];

        return new AggregatedMatchDto
        {
            MatchId      = reader.GetInt32(0),
            HomeTeam     = reader.GetString(1),
            AwayTeam     = reader.GetString(2),
            League       = reader.GetString(3),
            KickOff      = reader.GetFieldValue<DateTimeOffset>(4),
            Status       = reader.GetString(5),
            TotalTippers = (int)(long)reader.GetValue(6),
            Tips         = tips,
            Consensus    = BuildConsensus(tips),
            UpdatedAt    = DateTimeOffset.UtcNow
        };
    }

    private static AggregatedMatchDto MapToDto(AggregatedMatchEvent evt, string status) =>
        new()
        {
            MatchId      = evt.MatchId,
            HomeTeam     = evt.HomeTeam,
            AwayTeam     = evt.AwayTeam,
            League       = evt.League,
            KickOff      = evt.KickOff,
            Status       = status,
            TotalTippers = evt.TotalTippers,
            Consensus    = evt.Consensus,
            Tips         = evt.Tips.Select(t => new TipDetailDto
            {
                ChannelId    = t.ChannelId,
                ChannelTitle = t.ChannelTitle,
                TipType      = t.TipType,
                TipValue     = t.TipValue,
                Odds         = t.Odds
            }).ToList(),
            UpdatedAt = evt.UpdatedAt
        };

    private static Dictionary<string, Dictionary<string, double>> BuildConsensus(
        IReadOnlyList<TipDetailDto> tips)
    {
        var result = new Dictionary<string, Dictionary<string, double>>();
        foreach (var group in tips.GroupBy(t => t.TipType))
        {
            var total = group.Count();
            result[group.Key] = group
                .GroupBy(t => t.TipValue)
                .ToDictionary(g => g.Key, g => Math.Round((double)g.Count() / total, 4));
        }
        return result;
    }
}
