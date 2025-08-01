using System;
using MediatR;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Abstractions.IServices;

namespace src.Feature.User.Auth.Logout;

public class LogoutRequestHandler : IRequestHandler<LogoutRequest, Result<LogoutResponse, ErrorResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICookieService _cookieService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LogoutRequestHandler(IUnitOfWork unitOfWork, ICookieService cookieService, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _cookieService = cookieService ?? throw new ArgumentNullException(nameof(cookieService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task<Result<LogoutResponse, ErrorResponse>> Handle(LogoutRequest request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return Result<LogoutResponse, ErrorResponse>.Failure(ErrorResponse.Internal("An unexpected error occurred."));
        }

        var refreshToken = _cookieService.GetRefreshTokenFromCookies(httpContext);
        if (string.IsNullOrEmpty(refreshToken))
        {
            _cookieService.ClearAuthCookies(httpContext);
            return Result<LogoutResponse, ErrorResponse>.Failure(ErrorResponse.Unauthorized("Refresh token is not found in cookies."));
        }

        var session = await _unitOfWork.Sessions.GetSessionByRefreshTokenAsync(refreshToken, cancellationToken);
        if (session is null)
        {
            _cookieService.ClearAuthCookies(httpContext);
            return Result<LogoutResponse, ErrorResponse>.Failure(ErrorResponse.Unauthorized("Session not found for the provided refresh token."));
        }

        session.Revoke();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cookieService.ClearAuthCookies(httpContext);
        return Result<LogoutResponse, ErrorResponse>.Success(new LogoutResponse("You have been logged out successfully."));
    }
}