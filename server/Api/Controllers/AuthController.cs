using Api.Controllers;
using Application.Features.Auth.Login;
using Application.Features.Auth.Logout;
using Application.Features.Auth.RefreshToken;
using Application.Features.Auth.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace server.Api.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : BaseController
    {
        protected AuthController(IMediator mediator) : base(mediator) { }


        [HttpPost("login")]
        public async Task<IActionResult> Login(string email, string password, CancellationToken cancellationToken)
        {
            var commnad = new LoginCommand(email, password);
            return await SendRequest(commnad);
        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var commnad = new LogoutCommand();
            return await SendRequest(commnad);
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register(string userName, string email, string password, CancellationToken cancellationToken)
        {
            var commnad = new RegisterCommand(userName, email, password);
            return await SendRequest(commnad);
        }
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
        {
            var commnad = new RefreshTokenCommand();
            return await SendRequest(commnad);
        }

    }
}
