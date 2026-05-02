using Application.Common.Errors;
using Application.Common.Results;
using Application.Interfaces.Data;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth;

public class RegisterHandler(
    IDataBase db, 
    IPasswordService passwordService, 
    ITokenService tokenService, 
    ICookieService cookieService, 
    IHttpContextAccessor httpContextAccessor
) : ICommandHandler<RegisterCommand, RegisterResponse>
{
    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken ct)
    {
        var exists = await db.Users.ByEmail(request.email).AsNoTracking().AnyAsync(ct);
        if (exists) return Result<RegisterResponse>.Failure(UserError.DuplicateEmail);

        var passwordHash = passwordService.HashPassword(request.password);
        var user = User.Create(request.username, request.email, passwordHash);
        
        await db.Users.AddAsync(user, ct);

        // Automatically create a default workspace for the new user
        var defaultWorkspace = ProjectWorkspace.Create(
            name: $"{user.Name}'s Workspace",
            slug: $"{user.Name.ToLower().Replace(" ", "-")}-{Guid.NewGuid().ToString("N")[..4]}",
            description: "Your personal default workspace.",
            joinCode: null,
            color: null,
            icon: null,
            creatorId: user.Id
        );
        await db.Workspaces.AddAsync(defaultWorkspace, ct);

        // Best Practice: Automatically log in the user after registration
        var httpContext = httpContextAccessor.HttpContext;
        var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var tokens = tokenService.GenerateTokens(user, userAgent, ipAddress);
        var session = Session.Create(user.Id, tokens.RefreshToken, tokens.ExpirationRefreshToken, userAgent, ipAddress);
        
        await db.Sessions.AddAsync(session, ct);
        await db.SaveChangesAsync(ct);

        cookieService.SetAuthCookies(tokens);

        return Result<RegisterResponse>.Success(new RegisterResponse(user.Id, user.Name, user.Email));
    }
}
