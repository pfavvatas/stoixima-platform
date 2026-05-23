using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedController : ControllerBase
{
    private readonly FeedRepository _repo;

    public FeedController(FeedRepository repo) => _repo = repo;

    /// <summary>GET /api/feed — aggregated match feed</summary>
    [HttpGet]
    public async Task<IActionResult> GetFeed(
        [FromQuery] string? date,
        [FromQuery] string? league,
        [FromQuery] int minTippers = 1,
        CancellationToken ct = default)
    {
        var parsedDate = date is not null && DateOnly.TryParse(date, out var d)
            ? d
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var feed = await _repo.GetFeedAsync(parsedDate, league, minTippers, ct);
        return Ok(feed);
    }

    /// <summary>GET /api/feed/match/{matchId} — per-match details with per-tipster tips</summary>
    [HttpGet("match/{matchId:int}")]
    public async Task<IActionResult> GetMatch(int matchId, CancellationToken ct)
    {
        var match = await _repo.GetMatchDetailAsync(matchId, ct);
        if (match is null) return NotFound();
        return Ok(match);
    }
}
