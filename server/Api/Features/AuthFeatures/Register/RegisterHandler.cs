using Microsoft.EntityFrameworkCore;

namespace Api;

public class RegisterHandler(
    TaskPlanDbContext db,
    TokenService tokenService,
    CookieService cookieService,
    WorkspaceService workspaceService,
    IHttpContextAccessor httpContextAccessor
) : ICommandHandler<RegisterCommand, RegisterResponse>
{
    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Email == request.email, cancellationToken);
        if (exists) return Result<RegisterResponse>.Failure(UserError.DuplicateEmail);

        User? createdUser = null;
        ProjectWorkspace? defaultWorkspace = null;

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var passwordHash = PasswordService.HashPassword(request.password);
            var user = User.Create(request.username, request.email, passwordHash);
            createdUser = user;

            await db.Users.AddAsync(user, cancellationToken);

            defaultWorkspace = ProjectWorkspace.Create(
                name: $"{user.Name}'s Workspace",
                slug: $"{user.Name.ToLower().Replace(" ", "-")}-{Guid.NewGuid().ToString("N")[..4]}",
                description: "Your personal default workspace.",
                joinCode: null,
                color: null,
                icon: null,
                creatorId: user.Id
            );
            await db.ProjectWorkspaces.AddAsync(defaultWorkspace, cancellationToken);

            var httpContext = httpContextAccessor.HttpContext;
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var tokens = tokenService.GenerateTokens(user, userAgent, ipAddress);
            var session = Session.Create(user.Id, tokens.RefreshToken, tokens.ExpirationRefreshToken, userAgent, ipAddress);
            await db.Sessions.AddAsync(session, cancellationToken);
            cookieService.SetAuthCookies(tokens);
            return Result<RegisterResponse>.Success(new RegisterResponse(user.Id, user.Name, user.Email));
        }, cancellationToken);

        if (result.IsSuccess && defaultWorkspace != null && createdUser != null)
        {
            workspaceService.InitializeInBackground(defaultWorkspace.Id, createdUser.Id);
        }

        return result;
    }
}
