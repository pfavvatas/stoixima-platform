using Npgsql;

namespace MatchDataService.Services;

// Maps external API team names to internal team IDs via api_team_mappings table.
// Falls back to inserting a new team if no mapping exists.
public class TeamNameNormalizer
{
    private readonly string _connectionString;
    private readonly ILogger<TeamNameNormalizer> _logger;

    private const string LookupSql = """
        SELECT team_id FROM api_team_mappings
        WHERE api_name = @api_name AND api_source = 'football-data'
        """;

    private const string InsertTeamSql = """
        INSERT INTO teams (name, aliases, country, league)
        VALUES (@name, ARRAY[]::text[], @country, @league)
        ON CONFLICT (name, league) DO UPDATE SET name = EXCLUDED.name
        RETURNING id
        """;

    private const string InsertMappingSql = """
        INSERT INTO api_team_mappings (api_name, api_source, team_id)
        VALUES (@api_name, 'football-data', @team_id)
        ON CONFLICT (api_name, api_source) DO NOTHING
        """;

    public TeamNameNormalizer(IConfiguration configuration, ILogger<TeamNameNormalizer> logger)
    {
        _connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing Postgres connection string.");
        _logger = logger;
    }

    public async Task<int> GetOrCreateTeamIdAsync(
        NpgsqlConnection conn,
        string apiName,
        string league,
        CancellationToken ct)
    {
        // 1. Try existing mapping
        await using var lookupCmd = new NpgsqlCommand(LookupSql, conn);
        lookupCmd.Parameters.AddWithValue("@api_name", apiName);
        var existing = await lookupCmd.ExecuteScalarAsync(ct);
        if (existing is int teamId)
            return teamId;

        // 2. Insert team + mapping (auto-register unknown teams)
        _logger.LogInformation("Auto-registering unknown team: {ApiName} ({League})", apiName, league);

        await using var insertTeam = new NpgsqlCommand(InsertTeamSql, conn);
        insertTeam.Parameters.AddWithValue("@name",    apiName);
        insertTeam.Parameters.AddWithValue("@country", DBNull.Value);
        insertTeam.Parameters.AddWithValue("@league",  league);
        var newId = (int)(await insertTeam.ExecuteScalarAsync(ct) ?? throw new InvalidOperationException("INSERT returned null"));

        await using var insertMapping = new NpgsqlCommand(InsertMappingSql, conn);
        insertMapping.Parameters.AddWithValue("@api_name", apiName);
        insertMapping.Parameters.AddWithValue("@team_id",  newId);
        await insertMapping.ExecuteNonQueryAsync(ct);

        return newId;
    }
}
