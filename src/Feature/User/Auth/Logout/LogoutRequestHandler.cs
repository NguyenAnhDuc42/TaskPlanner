using System;
using MediatR;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Data;

namespace src.Feature.User.Auth.Logout;

public class LogoutRequestHandler : IRequestHandler<LogoutRequest, Result<LogoutResponse, ErrorResponse>>
{
    private readonly PlannerDbContext _context;
    private readonly ICookieService _cookieService;
    private readonly ISessionRepository _sessionRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public LogoutRequestHandler(ICookieService cookieService, ISessionRepository sessionRepository,IHttpContextAccessor httpContextAccessor,PlannerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _cookieService = cookieService ?? throw new ArgumentException(nameof(cookieService));
        _sessionRepository = sessionRepository ?? throw new ArgumentException(nameof(sessionRepository));
    }
    public async Task<Result<LogoutResponse, ErrorResponse>> Handle(LogoutRequest request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) return Result<LogoutResponse, ErrorResponse>.Failure(ErrorResponse.Internal("An unexpected error occurred."));
        var refreshToken = _cookieService.GetRefreshTokenFromCookies(httpContext);
        if (string.IsNullOrEmpty(refreshToken))
        {
            _cookieService.ClearAuthCookies(httpContext);
            return Result<LogoutResponse, ErrorResponse>.Failure(ErrorResponse.Unauthorized("Unauthorized", "Refresh token is not found in cookies."));
        }
        var session = await _sessionRepository.GetSessionByRefreshTokenAsync(refreshToken, cancellationToken);
        if (session is null)
        {
            _cookieService.ClearAuthCookies(httpContext);
            return Result<LogoutResponse, ErrorResponse>.Failure(ErrorResponse.Unauthorized("Unauthorized", "Session not found for the provided refresh token."));
        }
        session.Revoke();
        await _context.SaveChangesAsync(cancellationToken);
        _cookieService.ClearAuthCookies(httpContext);
        return Result<LogoutResponse, ErrorResponse>.Success(new LogoutResponse("You have been logged out successfully."));
        

    }
}
