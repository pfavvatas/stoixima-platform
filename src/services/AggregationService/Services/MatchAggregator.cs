using System.Text.Json;
using Contracts;
using Contracts.Events;
using Messaging;
using Npgsql;
using Prometheus;
using StackExchange.Redis;

namespace AggregationService.Services;

public class MatchAggregator
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IEventPublisher _publisher;
    private readonly string _connectionString;
    private readonly ILogger<MatchAggregator> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private static readonly Counter AggregationsTotal = Metrics.CreateCounter(
        "aggregation_calculations_total", "Total aggregation calculations performed");

    // In-memory buffer of tips per match, cleared after publishing
    private readonly Dictionary<int, MatchBuffer> _buffers = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    private const string LoadMatchSql = """
        SELECT t.channel_id, t.match_id, c.title, m.home_team_id, m.away_team_id,
               m.kick_off, m.league, t.tip_type, t.tip_value, t.odds, t.confidence, t.extracted_at,
               th.name AS home_name, ta.name AS away_name
        FROM   tips t
        JOIN   channels c  ON c.id = t.channel_id
        JOIN   matches  m  ON m.id = t.match_id
        JOIN   teams    th ON th.id = m.home_team_id
        JOIN   teams    ta ON ta.id = m.away_team_id
        WHERE  t.match_id = @match_id AND t.is_valid = true
        """;

    public MatchAggregator(
        IConnectionMultiplexer redis,
        IEventPublisher publisher,
        IConfiguration configuration,
        ILogger<MatchAggregator> logger)
    {
        _redis            = redis;
        _publisher        = publisher;
        _connectionString = configuration.GetConnectionString("Postgres")
                            ?? throw new InvalidOperationException("Missing Postgres connection string.");
        _logger           = logger;
    }

    public async Task OnTipReceivedAsync(ProcessedTipEvent tip, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!_buffers.TryGetValue(tip.MatchId, out var buffer))
            {
                buffer = new MatchBuffer(tip);
                _buffers[tip.MatchId] = buffer;
            }
            buffer.Add(tip);
        }
        finally { _lock.Release(); }

        await AggregateAndPublishAsync(tip.MatchId, ct);
    }

    public async Task AggregateAndPublishAsync(int matchId, CancellationToken ct)
    {
        // Load all tips for this match from Postgres to get full picture
        var tips = await LoadTipsFromDbAsync(matchId, ct);
        if (tips.Count == 0) return;

        var latest    = tips[0];
        var consensus = AggregationCalculator.Calculate(tips);
        var details   = AggregationCalculator.ToTipDetails(tips);

        var evt = new AggregatedMatchEvent
        {
            MatchId      = matchId,
            HomeTeam     = latest.HomeTeam,
            AwayTeam     = latest.AwayTeam,
            League       = latest.League,
            KickOff      = latest.KickOff,
            TotalTippers = tips.Select(t => t.ChannelId).Distinct().Count(),
            Consensus    = consensus,
            Tips         = details,
            UpdatedAt    = DateTimeOffset.UtcNow
        };

        // Write to Redis
        var db  = _redis.GetDatabase();
        var key = $"feed:match:{matchId}";
        var json = JsonSerializer.Serialize(evt);
        await db.StringSetAsync(key, json, CacheTtl);

        // Publish to Kafka
        await _publisher.PublishAsync(
            KafkaTopics.AggregatedFeed,
            matchId.ToString(),
            evt,
            ct);

        AggregationsTotal.Inc();
        _logger.LogInformation(
            "Aggregated match {MatchId} ({Home} vs {Away}): {Tippers} tippers",
            matchId, evt.HomeTeam, evt.AwayTeam, evt.TotalTippers);
    }

    private async Task<List<ProcessedTipEvent>> LoadTipsFromDbAsync(int matchId, CancellationToken ct)
    {
        var tips = new List<ProcessedTipEvent>();
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(LoadMatchSql, conn);
        cmd.Parameters.AddWithValue("@match_id", matchId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            tips.Add(new ProcessedTipEvent
            {
                ChannelId    = reader.GetInt64(0),
                MatchId      = reader.GetInt32(1),
                ChannelTitle = reader.GetString(2),
                HomeTeam     = reader.GetString(12),
                AwayTeam     = reader.GetString(13),
                KickOff      = reader.GetFieldValue<DateTimeOffset>(5),
                League       = reader.GetString(6),
                TipType      = reader.GetString(7),
                TipValue     = reader.GetString(8),
                Odds         = reader.IsDBNull(9) ? null : reader.GetDecimal(9),
                Confidence   = reader.IsDBNull(10) ? 0 : (double)reader.GetDecimal(10),
                Timestamp    = reader.GetFieldValue<DateTimeOffset>(11),
            });
        }

        return tips;
    }
}

internal class MatchBuffer
{
    private readonly ProcessedTipEvent _first;
    private readonly List<ProcessedTipEvent> _tips = [];

    public MatchBuffer(ProcessedTipEvent first) => _first = first;
    public void Add(ProcessedTipEvent tip) => _tips.Add(tip);
}
