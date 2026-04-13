using Application.Common.Errors;
using Application.Common.Results;
using Application.Interfaces.Data;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Auth.Register;

public class RegisterHandler(
    IDataBase db, 
    IPasswordService passwordService, 
    ITokenService tokenService, 
    ICookieService cookieService, 
    IHttpContextAccessor httpContextAccessor, 
    ILogger<RegisterHandler> logger
) : ICommandHandler<RegisterCommand, RegisterResponse>
{
    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken ct)
    {
        logger.LogInformation("Registering new user: {Email}", request.email);

        var exists = await db.Users.AnyAsync(u => u.Email == request.email, ct);
        if (exists)
        {
            logger.LogWarning("Registration failed: Email {Email} already exists", request.email);
            return Result<RegisterResponse>.Failure(UserError.DuplicateEmail);
        }

        var passwordHash = passwordService.HashPassword(request.password);
        var user = User.Create(request.username, request.email, passwordHash);
        
        await db.Users.AddAsync(user, ct);
        await db.SaveChangesAsync(ct);

        // Auto-login flow
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null) 
            return Result<RegisterResponse>.Failure(Error.Failure("Auth.ContextError", "No HttpContext available for registration login."));

        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

        var tokens = tokenService.GenerateTokens(user, userAgent, ipAddress);
        cookieService.SetAuthCookies(httpContext, tokens);

        // Standardized record usage while preserving original DateTimeOffset types in sessions
        var session = Session.Create(user.Id, tokens.RefreshToken, tokens.ExpirationRefreshToken, userAgent, ipAddress);
        await db.Sessions.AddAsync(session, ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation("User registered and logged in: {UserId}, {Email}", user.Id, user.Email);

        return Result<RegisterResponse>.Success(new RegisterResponse(user.Id, user.Name, user.Email));
    }
}
