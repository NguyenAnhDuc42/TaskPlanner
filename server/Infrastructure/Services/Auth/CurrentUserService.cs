
using System.Security.Claims;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Application.Interfaces;

namespace Infrastructure.Auth;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TaskPlanDbContext _dbContext;
    private readonly ILogger<CurrentUserService> _logger;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, TaskPlanDbContext dbContext, ILogger<CurrentUserService> logger)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public User CurrentUser()
    {
        var user = _dbContext.Users.Find(CurrentUserId());
        if (user == null) throw new UnauthorizedAccessException("Unauthorized");
        return user!;
    }

    public async Task<User> CurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { CurrentUserId() }, cancellationToken);
        if (user == null) throw new UnauthorizedAccessException("Unauthorized");
        return user!;
    }

    public Guid CurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return Guid.Empty;

        var user = httpContext.User;
        
        // Find by both standard ClaimTypes and JWT 'sub'
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? 
                          user.FindFirst("sub");

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                var claims = string.Join(", ", httpContext.User.Claims.Select(c => $"{c.Type}={c.Value}"));
                _logger.LogWarning("[Diagnostic] User is authenticated but ID claim ('{IdType}' or 'sub') is missing or invalid. Claims present: {Claims}", 
                    ClaimTypes.NameIdentifier, claims);
            }
            else
            {
                _logger.LogDebug("[Diagnostic] User is not authenticated.");
            }
            return Guid.Empty;
        }
        return userId;
    }

    public Guid UserId
    {
        get => CurrentUserId();
    }
}



