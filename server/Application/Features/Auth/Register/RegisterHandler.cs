using Application.Common.Errors;
using Application.Interfaces.Data;
using Application.Common.Results;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using server.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Application.Features.Auth.Register;

public class RegisterHandler : ICommandHandler<RegisterCommand, RegisterResponse>
{
    private readonly IDataBase _db;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly ICookieService _cookieService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RegisterHandler> _logger;

    public RegisterHandler(
        IDataBase db, 
        IPasswordService passwordService, 
        ITokenService tokenService, 
        ICookieService cookieService, 
        IHttpContextAccessor httpContextAccessor, 
        ILogger<RegisterHandler> logger)
    {
        _db = db;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _cookieService = cookieService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Registering new user: {Email}", request.email);

        var exists = await _db.Users.AnyAsync(u => u.Email == request.email, ct);
        if (exists)
        {
            _logger.LogWarning("Registration failed: Email {Email} already exists", request.email);
            return UserError.DuplicateEmail;
        }

        var passwordHash = _passwordService.HashPassword(request.password);
        var user = User.Create(request.username, request.email, passwordHash);
        
        await _db.Users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);

        // Auto-login flow
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) 
            return Error.Failure("Auth.ContextError", "No HttpContext available for registration login.");

        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

        var tokens = _tokenService.GenerateTokens(user, userAgent, ipAddress);
        _cookieService.SetAuthCookies(httpContext, tokens);

        var session = Session.Create(user.Id, tokens.RefreshToken, tokens.ExpirationRefreshToken, userAgent, ipAddress);
        await _db.Sessions.AddAsync(session, ct);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User registered and logged in: {UserId}, {Email}", user.Id, user.Email);

        return new RegisterResponse(user.Id, user.Name, user.Email);
    }
}
