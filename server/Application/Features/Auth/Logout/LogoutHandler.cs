using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using server.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.Logout
{
    public class LogoutHandler : IRequestHandler<LogoutCommand, LogoutResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICookieService _cookieService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<LogoutHandler> _logger;

        public LogoutHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ICookieService cookieService, IHttpContextAccessor httpContextAccessor, ILogger<LogoutHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _cookieService = cookieService ?? throw new ArgumentNullException(nameof(cookieService));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<LogoutResponse> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx is null)
            {
                _logger.LogError("LogoutHandler: HttpContext is null.");
                throw new InvalidOperationException("HttpContext is unavailable.");
            }

            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var ua = ctx.Request.Headers["User-Agent"].ToString() ?? "unknown";

            string? refreshToken = null;
            refreshToken = _cookieService.GetRefreshTokenFromCookies(ctx);
            _cookieService.ClearAuthCookies(ctx);

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogInformation("Logout invoked with no refresh token. IP:{Ip} UA:{UA}", ip, ua);
                return new LogoutResponse("Logout successful.");
            }

            var user = _currentUserService.CurrentUserWithSession();
            user.Logout(refreshToken, DateTimeOffset.UtcNow);

            _logger.LogInformation("Logout: revoked session {RefreshToken} for user {UserId} from IP {Ip}", refreshToken,user.Id, ip);
            _unitOfWork.Set<User>().Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new LogoutResponse("Logout successful.");
        }
    }
}
