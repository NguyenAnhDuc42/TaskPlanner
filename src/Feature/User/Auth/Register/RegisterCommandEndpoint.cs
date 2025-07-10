
using Microsoft.AspNetCore.Mvc;
using src.Feature.User.Auth.Register;
using src.Helper.Results;

namespace src.Feature.User.Auth
{
    public partial class AuthController
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return result.ToApiResult();
        }
    }
}
