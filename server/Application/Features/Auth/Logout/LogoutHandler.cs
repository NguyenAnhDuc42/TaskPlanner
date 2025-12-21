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
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _cookieService = cookieService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<LogoutResponse> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var ctx = _httpContextAccessor.HttpContext 
                ?? throw new InvalidOperationException("HttpContext unavailable");

            var refreshToken = _cookieService.GetRefreshTokenFromCookies(ctx);
            _cookieService.ClearAuthCookies(ctx);

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogInformation("Logout with no refresh token");
                return new LogoutResponse("Logout successful");
            }

            var userId = _currentUserService.CurrentUserId();
            var session = await _unitOfWork.Set<Session>()
                .FirstOrDefaultAsync(s => s.UserId == userId && s.RefreshToken == refreshToken, cancellationToken);

            if (session != null)
            {
                session.Revoke(DateTimeOffset.UtcNow);
                _unitOfWork.Set<Session>().Update(session);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Session revoked for user {UserId}", userId);
            }

            return new LogoutResponse("Logout successful");
        }
    }   
}
