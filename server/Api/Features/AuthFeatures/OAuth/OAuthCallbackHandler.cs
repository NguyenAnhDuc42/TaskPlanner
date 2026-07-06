using Microsoft.EntityFrameworkCore;

namespace Api;

public class OAuthCallbackHandler(
    TaskPlanDbContext db,
    TokenService tokenService,
    CookieService cookieService,
    IHttpContextAccessor httpContextAccessor
) : ICommandHandler<OAuthCallbackCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(OAuthCallbackCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (user is null)
            {
                user = User.CreateExternal(request.Name, request.Email, request.Provider, request.ExternalId);
                await db.Users.AddAsync(user, cancellationToken);

                var slug = $"{request.Name.ToLower().Replace(" ", "-")}-{Guid.NewGuid().ToString("N")[..4]}";
                var workspace = ProjectWorkspace.Create(
                    name: $"{user.Name}'s Workspace",
                    slug: slug,
                    description: "Your personal default workspace.",
                    joinCode: null,
                    color: null,
                    icon: null,
                    creatorId: user.Id
                );
                await db.ProjectWorkspaces.AddAsync(workspace, cancellationToken);
            }
            else if (!user.IsLinkedToProvider(request.Provider))
            {
                user.LinkExternalAccount(request.Provider, request.ExternalId);
            }

            var httpContext = httpContextAccessor.HttpContext;
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var tokens = tokenService.GenerateTokens(user, userAgent, ipAddress);
            var session = Session.Create(user.Id, tokens.RefreshToken, tokens.ExpirationRefreshToken, userAgent, ipAddress);

            await db.Sessions.AddAsync(session, cancellationToken);
            cookieService.SetAuthCookies(tokens);

            return Result<LoginResponse>.Success(new LoginResponse(tokens.ExpirationAccessToken, tokens.ExpirationRefreshToken));
        }, cancellationToken);

        return result;
    }
}
