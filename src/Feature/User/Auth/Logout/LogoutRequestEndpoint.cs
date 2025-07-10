using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Feature.User.Auth.Logout;
using src.Helper.Results;

namespace src.Feature.User.Auth
{
    public partial class AuthController
    {
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return result.ToApiResult();
        }
    }
}

