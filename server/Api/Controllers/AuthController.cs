using Application.Features.Auth.Login;
using Application.Features.Auth.Logout;
using Application.Features.Auth.RefreshToken;
using Application.Features.Auth.Register;
using Application.Features.Auth.ForgotPassword;
using Application.Features.Auth.ResetPassword;
using Application.Features.Auth.OAuth;
using Application.Features.Auth.ChangePassword;
using Application.Features.Auth.GetCurrentUser;
using Application.Features.Auth.UpdateProfile;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Api.Extensions;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IHandler _handler;

    public AuthController(IHandler iHandler)
    {
        _handler = iHandler;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _handler.SendAsync<LoginCommand, LoginResponse>(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var result = await _handler.SendAsync(new LogoutCommand(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.UserName, request.Email, request.Password);
        var result = await _handler.SendAsync<RegisterCommand, RegisterResponse>(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var result = await _handler.SendAsync<RefreshTokenCommand, RefreshTokenResponse>(new RefreshTokenCommand(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _handler.SendAsync<ForgotPasswordCommand, string?>(new ForgotPasswordCommand(request.Email), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(request.Token, request.NewPassword);
        var result = await _handler.SendAsync(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("external-login")]
    public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginRequest request, CancellationToken cancellationToken)
    {
        var command = new ExternalLoginCommand(request.Provider, request.Token);
        var result = await _handler.SendAsync<ExternalLoginCommand, LoginResponse>(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("change-password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ChangePasswordCommand(request.CurrentPassword, request.NewPassword);
        var result = await _handler.SendAsync(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var result = await _handler.QueryAsync<GetCurrentUserQuery, GetCurrentUserDto>(new GetCurrentUserQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("profile")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateProfileCommand(request.Name, request.Email);
        var result = await _handler.SendAsync<UpdateProfileCommand, UpdateProfileDto>(command, cancellationToken);
        return result.ToActionResult();
    }
}

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string UserName, string Email, string Password);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
public record ExternalLoginRequest(string Provider, string Token);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record UpdateProfileRequest(string? Name, string? Email);
