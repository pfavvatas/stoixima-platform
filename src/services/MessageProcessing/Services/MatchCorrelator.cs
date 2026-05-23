using Npgsql;

namespace MessageProcessing.Services;

public class MatchCorrelator
{
    private readonly string _connectionString;
    private readonly ILogger<MatchCorrelator> _logger;

    private const string FindMatchSql = """
        SELECT m.id, t1.name, t2.name, m.kick_off, m.league
        FROM   matches m
        JOIN   teams t1 ON t1.id = m.home_team_id
        JOIN   teams t2 ON t2.id = m.away_team_id
        WHERE  m.home_team_id = @home
          AND  m.away_team_id = @away
          AND  DATE(m.kick_off) BETWEEN CURRENT_DATE AND CURRENT_DATE + INTERVAL '1 day'
          AND  m.status IN ('scheduled', 'live')
        ORDER  BY m.kick_off
        LIMIT  1
        """;

    public MatchCorrelator(IConfiguration configuration, ILogger<MatchCorrelator> logger)
    {
        _connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing Postgres connection string.");
        _logger = logger;
    }

    public async Task<MatchInfo?> FindAsync(int homeTeamId, int awayTeamId, CancellationToken ct)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new NpgsqlCommand(FindMatchSql, conn);
            cmd.Parameters.AddWithValue("@home", homeTeamId);
            cmd.Parameters.AddWithValue("@away", awayTeamId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return null;

            return new MatchInfo(
                Id:       reader.GetInt32(0),
                HomeTeam: reader.GetString(1),
                AwayTeam: reader.GetString(2),
                KickOff:  reader.GetFieldValue<DateTimeOffset>(3),
                League:   reader.GetString(4));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MatchCorrelator DB error for teams ({Home}, {Away})", homeTeamId, awayTeamId);
            return null;
        }
    }
}

public record MatchInfo(int Id, string HomeTeam, string AwayTeam, DateTimeOffset KickOff, string League);
