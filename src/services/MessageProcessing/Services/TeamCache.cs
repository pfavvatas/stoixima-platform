using Npgsql;

namespace MessageProcessing.Services;

public class TeamCache : ITeamCache, IHostedService, IDisposable
{
    private readonly string _connectionString;
    private readonly ILogger<TeamCache> _logger;
    private IReadOnlyList<TeamEntry> _teams = [];
    private Timer? _refreshTimer;

    public IReadOnlyList<TeamEntry> Teams => _teams;

    public TeamCache(IConfiguration configuration, ILogger<TeamCache> logger)
    {
        _connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing Postgres connection string.");
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RefreshAsync(cancellationToken);

        // Refresh the in-memory cache once an hour so new teams propagate without a restart.
        _refreshTimer = new Timer(
            _ => _ = RefreshAsync(CancellationToken.None),
            null,
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _refreshTimer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new NpgsqlCommand(
                "SELECT id, name, aliases, country, league FROM teams ORDER BY id", conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            var teams = new List<TeamEntry>();
            while (await reader.ReadAsync(ct))
            {
                teams.Add(new TeamEntry(
                    Id:      reader.GetInt32(0),
                    Name:    reader.GetString(1),
                    Aliases: reader.GetFieldValue<string[]>(2),
                    Country: reader.IsDBNull(3) ? null : reader.GetString(3),
                    League:  reader.IsDBNull(4) ? null : reader.GetString(4)));
            }

            _teams = teams;
            _logger.LogInformation("TeamCache: loaded {Count} teams", teams.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TeamCache: failed to refresh from Postgres");
        }
    }

    public void Dispose() => _refreshTimer?.Dispose();
}
