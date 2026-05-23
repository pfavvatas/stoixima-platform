using AggregationService.Services;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace AggregationService.Controllers;

[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly MatchAggregator _aggregator;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        MatchAggregator aggregator,
        IConnectionMultiplexer redis,
        ILogger<AdminController> logger)
    {
        _aggregator = aggregator;
        _redis      = redis;
        _logger     = logger;
    }

    [HttpPost("re-aggregate")]
    public async Task<IActionResult> ReAggregate(CancellationToken ct)
    {
        _logger.LogInformation("Manual re-aggregation triggered via admin API");

        var db      = _redis.GetDatabase();
        var key     = $"matches:today:{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}";
        var members = await db.SetMembersAsync(key);
        var matchIds = members.Select(m => (int)(long)m).ToList();

        var results = new List<object>();

        foreach (var matchId in matchIds)
        {
            try
            {
                await _aggregator.AggregateAndPublishAsync(matchId, ct);
                results.Add(new { matchId, status = "ok" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Re-aggregation failed for match {MatchId}", matchId);
                results.Add(new { matchId, status = "error", error = ex.Message });
            }
        }

        return Ok(new { reAggregated = results, total = matchIds.Count });
    }
}
