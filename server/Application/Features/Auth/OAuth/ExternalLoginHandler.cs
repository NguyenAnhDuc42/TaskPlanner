using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
namespace Application;

public class ExternalLoginHandler : ICommandHandler<ExternalLoginCommand, LoginResponse>
{
    private readonly TaskPlanDbContext _db;
    private readonly ExternalAuthService _externalAuthService;
    private readonly TokenService _tokenService;
    private readonly CookieService _cookieService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ExternalLoginHandler(
        TaskPlanDbContext db, 
        ExternalAuthService externalAuthService,
        TokenService tokenService,
        CookieService cookieService,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _externalAuthService = externalAuthService;
        _tokenService = tokenService;
        _cookieService = cookieService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<LoginResponse>> Handle(ExternalLoginCommand request, CancellationToken ct)
    {
        // 1. Validate Token with Provider
        var externalUser = await _externalAuthService.ValidateAsync(request.Provider, request.Token);

        // 2. Find or Create User
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == externalUser.Email, ct);
        
        if (user == null)
        {
            // JIT Provisioning
            user = User.CreateExternal(externalUser.Name, externalUser.Email, externalUser.Provider, externalUser.ExternalId);
            await _db.Users.AddAsync(user, ct);
            await _db.SaveChangesAsync(ct);
        }
        else if (!user.IsLinkedToProvider(request.Provider))
        {
            // Existing user, link OAuth account
            user.LinkExternalAccount(request.Provider, externalUser.ExternalId);
            await _db.SaveChangesAsync(ct);
        }

        // 3. Generate Tokens and Create Session
        var httpContext = _httpContextAccessor.HttpContext;
        var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var tokens = _tokenService.GenerateTokens(user, userAgent, ipAddress);
        
        var session = Session.Create(user.Id, tokens.RefreshToken, tokens.ExpirationRefreshToken, userAgent, ipAddress);
        await _db.Sessions.AddAsync(session, ct);
        await _db.SaveChangesAsync(ct);

        // Update Cookies
        _cookieService.SetAuthCookies(tokens);

        return Result<LoginResponse>.Success(new LoginResponse(tokens.ExpirationAccessToken, tokens.ExpirationRefreshToken));
    }
}



