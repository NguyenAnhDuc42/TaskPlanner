using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using server.Application.Interfaces;

namespace Application.Features.Auth.Logout
{
    public class LogoutHandler : IRequestHandler<LogoutCommand, LogoutResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICookieService _cookieService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<LogoutHandler> _logger;

        public LogoutHandler(
            IUnitOfWork unitOfWork,
            ICookieService cookieService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<LogoutHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
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
                // In practice this should not happen; surface as internal error so it can be investigated.
                throw new InvalidOperationException("HttpContext is unavailable.");
            }

            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var ua = ctx.Request.Headers["User-Agent"].ToString() ?? "unknown";

            // Read refresh token (raw token) then immediately clear cookies to reduce replay window
            string? refreshToken = null;
            try
            {
                refreshToken = _cookieService.GetRefreshTokenFromCookies(ctx);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read refresh token from cookies during logout.");
                // proceed to clear cookies anyway
            }

            try
            {
                _cookieService.ClearAuthCookies(ctx);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clear auth cookies during logout.");
                // continue, still attempt to revoke session server-side if possible
            }

            // Silent success path if no token provided
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogInformation("Logout invoked with no refresh token. IP:{Ip} UA:{UA}", ip, ua);
                return new LogoutResponse("Logout successful.");
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var session = await _unitOfWork.Set<Session>().FirstOrDefaultAsync(s => s.RefreshToken == refreshToken, cancellationToken);

                if (session != null)
                {
                    // Mark revoked (simple, cheap DB update). Delete can be deferred to background job.
                    session.Revoke(reason: "user_logout", revokedAt: DateTime.UtcNow);
                    _unitOfWork.Set<Session>().Update(session);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    _logger.LogInformation("Logout: revoked session {SessionId} for user {UserId} from IP {Ip}", session.Id, session.UserId, ip);
                    return new LogoutResponse("Logout successful.");
                }

                // Session not found: commit (no-op) and return silent success
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                _logger.LogInformation("Logout: refresh token presented but no session found. IP:{Ip}", ip);
                return new LogoutResponse("Logout successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LogoutHandler: error while revoking session. IP:{Ip}", ip);
                try { await _unitOfWork.RollbackTransactionAsync(cancellationToken); } catch { /* swallow */ }
                // Do not expose internal error to client — return success to avoid probing
                return new LogoutResponse("Logout successful.");
            }
        }
    }
}
