using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Feature.User.Auth.Me;
using src.Helper.Results;

namespace src.Feature.User.Auth;

public partial class AuthController
{
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new MeRequest(), cancellationToken);
        return result.ToApiResult();
    }
} 