using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Feature.User.Auth.RefreshToken;
using src.Helper.Results;

namespace src.Feature.User.Auth;


public partial class AuthController
{
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        RefreshTokenRequest command = new RefreshTokenRequest();
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToApiResult();
    }
}

