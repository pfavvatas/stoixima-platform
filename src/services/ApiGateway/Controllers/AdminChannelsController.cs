using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("admin/channels")]
public class AdminChannelsController : ControllerBase
{
    private readonly AdminRepository _repo;

    public AdminChannelsController(AdminRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _repo.GetAllChannelsAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChannelRequest req, CancellationToken ct)
    {
        if (req.ChatId == 0 || string.IsNullOrWhiteSpace(req.Title))
            return BadRequest("chatId and title are required.");

        await _repo.CreateChannelAsync(req.ChatId, req.Title, req.Username, ct);
        return Created($"/admin/channels/{req.ChatId}", null);
    }

    [HttpPatch("{id:long}/active")]
    public async Task<IActionResult> Toggle(long id, [FromBody] ToggleActiveRequest req, CancellationToken ct)
    {
        await _repo.SetChannelActiveAsync(id, req.Active, ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _repo.DeleteChannelAsync(id, ct);
        return NoContent();
    }
}

public record CreateChannelRequest(long ChatId, string Title, string? Username);
public record ToggleActiveRequest(bool Active);
