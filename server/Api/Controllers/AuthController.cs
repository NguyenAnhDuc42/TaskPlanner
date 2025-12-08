using Api.Controllers;
using Application.Features.Auth.Login;
using Application.Features.Auth.Logout;
using Application.Features.Auth.RefreshToken;
using Application.Features.Auth.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace server.Api.Controllers;
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var command = new LogoutCommand();
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.UserName, request.Email, request.Password);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand();
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}

// Request Models
public record LoginRequest(string Email, string Password);
public record RegisterRequest(string UserName, string Email, string Password);
