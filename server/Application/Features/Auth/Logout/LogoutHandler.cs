using Application.Interfaces.Data;
using Application.Common.Results;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.Logout;

public class LogoutHandler : ICommandHandler<LogoutCommand>
{
    private readonly IDataBase _db;
    private readonly ICookieService _cookieService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<LogoutHandler> _logger;

    public LogoutHandler(
        IDataBase db, 
        ICookieService cookieService, 
        IHttpContextAccessor httpContextAccessor, 
        ILogger<LogoutHandler> logger)
    {
        _db = db;
        _cookieService = cookieService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken ct)
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx == null) 
            return Result.Success();

        var refreshToken = _cookieService.GetRefreshTokenFromCookies(ctx);
        _cookieService.ClearAuthCookies(ctx);

        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result.Success();

        var session = await _db.Sessions.ByRefreshToken(refreshToken).FirstOrDefaultAsync(ct);

        if (session != null)
        {
            session.Revoke(DateTimeOffset.UtcNow);
            await _db.SaveChangesAsync(ct);
        }

        return Result.Success();
    }
}
