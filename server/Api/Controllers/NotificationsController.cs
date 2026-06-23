using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Api;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationsController(IHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20,
        [FromQuery] bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.QueryAsync<GetNotificationsQuery, GetNotificationsResponse>(
            new GetNotificationsQuery(cursor, limit, unreadOnly), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("read")]
    public async Task<IActionResult> MarkRead(
        [FromBody] MarkNotificationsReadCommand command,
        CancellationToken cancellationToken)
    {
        var result = await handler.SendAsync(command, cancellationToken);
        return result.ToActionResult();
    }
}
