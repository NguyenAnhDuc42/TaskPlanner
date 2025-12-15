using Api.Controllers;
using Application.Features.Auth.Login;
using Application.Features.Auth.Logout;
using Application.Features.Auth.RefreshToken;
using Application.Features.Auth.Register;
using Application.Features.Auth.ForgotPassword;
using Application.Features.Auth.ResetPassword;
using Application.Features.Auth.OAuth;
using Application.Features.Auth.ChangePassword;
using Application.Features.Auth.GetCurrentUser;
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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ForgotPasswordCommand(request.Email);
        var token = await _mediator.Send(command, cancellationToken);
        
        if (token == null)
        {
             // Security: Don't reveal user existence, but since this is now dev-mode friendly, 
             // maybe we just say sent? Or actually since we cant send email, returning OK is fine.
             return Ok(new { message = "If the email exists, a reset token has been generated (check logs or response if dev mode)." });
        }

        // Return token directly for convenience since email is disabled
        return Ok(new { message = "Reset token generated.", token });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(request.Token, request.NewPassword);
        await _mediator.Send(command, cancellationToken);
        return Ok(new { message = "Password reset successfully." });
    }

    [HttpPost("external-login")]
    public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginRequest request, CancellationToken cancellationToken)
    {
        var command = new ExternalLoginCommand(request.Provider, request.Token);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }



    [HttpPost("change-password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ChangePasswordCommand(request.CurrentPassword, request.NewPassword);
        await _mediator.Send(command, cancellationToken);
        return Ok(new { message = "Password changed successfully." });
    }

    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var query = new GetCurrentUserQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}

// Request Models
public record LoginRequest(string Email, string Password);
public record RegisterRequest(string UserName, string Email, string Password);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
public record ExternalLoginRequest(string Provider, string Token);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
