using Application.Common.Interfaces;
using Application.Features.UserFeatures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.Extensions;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IHandler _handler;

    public UsersController(IHandler handler)
    {
        _handler = handler;
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var result = await _handler.QueryAsync<GetUserPreferenceQuery, UserPreferenceDto>(new GetUserPreferenceQuery(), ct);
        return result.ToActionResult();
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdateUserPreferenceCommand command, CancellationToken ct)
    {
        var result = await _handler.SendAsync(command, ct);
        return result.ToActionResult();
    }
}
