using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.User.Auth.Login;
using src.Helper.Results;

namespace src.Feature.User.Auth;

public partial class AuthController
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {

        var result = await _mediator.Send(request, cancellationToken);
        return result.ToApiResult();

    }
}
