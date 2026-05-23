using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly AdminRepository _repo;
    private readonly ServiceHealthChecker _health;
    private readonly ServiceActionClient _actions;

    public AdminController(
        AdminRepository repo,
        ServiceHealthChecker health,
        ServiceActionClient actions)
    {
        _repo    = repo;
        _health  = health;
        _actions = actions;
    }

    /// <summary>GET /admin/services — health of all microservices</summary>
    [HttpGet("services")]
    public async Task<IActionResult> GetServices(CancellationToken ct) =>
        Ok(await _health.CheckAllAsync(ct));

    /// <summary>GET /admin/stats — today's counts</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct) =>
        Ok(await _repo.GetStatsAsync(ct));

    /// <summary>POST /admin/actions/fetch-matches — trigger immediate match fetch</summary>
    [HttpPost("actions/fetch-matches")]
    public async Task<IActionResult> FetchMatches(CancellationToken ct)
    {
        var result = await _actions.TriggerAsync("match-data-service", "admin/fetch-now", ct);
        return result ? Ok(new { message = "Match fetch triggered." }) : StatusCode(503, "Service unavailable.");
    }

    /// <summary>POST /admin/actions/re-aggregate — re-aggregate all today's matches</summary>
    [HttpPost("actions/re-aggregate")]
    public async Task<IActionResult> ReAggregate(CancellationToken ct)
    {
        var result = await _actions.TriggerAsync("aggregation-service", "admin/re-aggregate", ct);
        return result ? Ok(new { message = "Re-aggregation triggered." }) : StatusCode(503, "Service unavailable.");
    }
}
