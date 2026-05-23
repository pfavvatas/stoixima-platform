using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChannelsController : ControllerBase
{
    private readonly FeedRepository _repo;

    public ChannelsController(FeedRepository repo) => _repo = repo;

    /// <summary>GET /api/channels — active channels list</summary>
    [HttpGet]
    public async Task<IActionResult> GetChannels(CancellationToken ct)
    {
        var channels = await _repo.GetChannelsAsync(ct);
        return Ok(channels);
    }
}
