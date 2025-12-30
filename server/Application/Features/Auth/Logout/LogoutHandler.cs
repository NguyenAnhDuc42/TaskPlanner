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
        private readonly ICookieService _cookieService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<LogoutHandler> _logger;

        public LogoutHandler(IUnitOfWork unitOfWork, ICookieService cookieService, IHttpContextAccessor httpContextAccessor, ILogger<LogoutHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _cookieService = cookieService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<LogoutResponse> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx == null) return new LogoutResponse("Logout successful");

            var refreshToken = _cookieService.GetRefreshTokenFromCookies(ctx);
            _cookieService.ClearAuthCookies(ctx);

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return new LogoutResponse("Logout successful");
            }

            // Find session by token alone so we can revoke it even if Access Token is dead
            var session = await _unitOfWork.Set<Session>()
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken, cancellationToken);

            if (session != null)
            {
                session.Revoke(DateTimeOffset.UtcNow);
                _unitOfWork.Set<Session>().Update(session);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new LogoutResponse("Logout successful");
        }
    }   
}
