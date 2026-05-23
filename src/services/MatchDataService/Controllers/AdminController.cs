using MatchDataService.Services;
using Microsoft.AspNetCore.Mvc;

namespace MatchDataService.Controllers;

[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly FootballApiClient _apiClient;
    private readonly MatchRepository _repository;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        FootballApiClient apiClient,
        MatchRepository repository,
        ILogger<AdminController> logger)
    {
        _apiClient  = apiClient;
        _repository = repository;
        _logger     = logger;
    }

    [HttpPost("fetch-now")]
    public async Task<IActionResult> FetchNow(CancellationToken ct)
    {
        _logger.LogInformation("Manual fetch triggered via admin API");

        var leagues = new[] { "PL", "PD", "SA", "BL1", "FL1" };
        var results = new List<object>();

        foreach (var league in leagues)
        {
            try
            {
                var matches = await _apiClient.GetTodayMatchesAsync(league, ct);
                await _repository.UpsertAsync(matches, league, ct);
                results.Add(new { league, count = matches.Count, status = "ok" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fetch failed for league {League}", league);
                results.Add(new { league, count = 0, status = "error", error = ex.Message });
            }
        }

        return Ok(new { fetched = results });
    }
}
