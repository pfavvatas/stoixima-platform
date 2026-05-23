using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchesController : ControllerBase
{
    private readonly FeedRepository _repo;

    public MatchesController(FeedRepository repo) => _repo = repo;

    /// <summary>GET /api/matches — matches by date</summary>
    [HttpGet]
    public async Task<IActionResult> GetMatches(
        [FromQuery] string? date,
        CancellationToken ct)
    {
        var parsedDate = date is not null && DateOnly.TryParse(date, out var d)
            ? d
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var matches = await _repo.GetMatchesAsync(parsedDate, ct);
        return Ok(matches);
    }
}
