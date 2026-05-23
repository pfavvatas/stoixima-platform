using System.Text.Json;
using MatchDataService.Configuration;
using MatchDataService.Models;
using Microsoft.Extensions.Options;

namespace MatchDataService.Services;

public class FootballApiClient
{
    private readonly HttpClient _http;
    private readonly FootballApiOptions _options;
    private readonly ILogger<FootballApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FootballApiClient(
        HttpClient http,
        IOptions<FootballApiOptions> options,
        ILogger<FootballApiClient> logger)
    {
        _http    = http;
        _options = options.Value;
        _logger  = logger;
    }

    // Fetches today's matches for a given league code (e.g. "PL", "PD").
    public async Task<List<ApiMatch>> GetTodayMatchesAsync(string leagueCode, CancellationToken ct)
    {
        var date  = DateOnly.FromDateTime(DateTime.UtcNow);
        var url   = $"/v4/competitions/{leagueCode}/matches?dateFrom={date:yyyy-MM-dd}&dateTo={date:yyyy-MM-dd}";

        _logger.LogDebug("Fetching matches: {Url}", url);

        var response = await _http.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Football API returned {Status} for {League}", response.StatusCode, leagueCode);
            return [];
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        var result  = JsonSerializer.Deserialize<FootballApiResponse>(content, JsonOptions);
        return result?.Matches ?? [];
    }
}
