using System;
using MediatR;
using src.Domain.Valueobject;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Services;

namespace src.Feature.User.Auth.Login;

public class LoginRequestHandler : IRequestHandler<LoginRequest, Result<LoginResponse, ErrorResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly ICookieService _cookieService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<LoginRequestHandler> _logger;
    public LoginRequestHandler(IUserRepository userRepository, IPasswordService passwordService, ILogger<LoginRequestHandler> logger, ITokenService tokenService, ICookieService cookieService, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _cookieService = cookieService ?? throw new ArgumentNullException(nameof(cookieService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
    }
    public async Task<Result<LoginResponse, ErrorResponse>> Handle(LoginRequest request ,CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                _logger.LogError("Unable to get HttpContext from IHttpContextAccessor.");
                return Result<LoginResponse, ErrorResponse>.Failure(ErrorResponse.Internal("An unexpected error occurred."));
            }
            var email = new Email(request.email);
            var user = await _userRepository.GetUserByEmailAsync(email, cancellationToken).ConfigureAwait(false);
            if (user is null)
            {
                return Result<LoginResponse, ErrorResponse>.Failure(ErrorResponse.NotFound("User not found", $"Wrong credentials"));
            }
            if (_passwordService.VerifyPassword(request.password, user.PasswordHash) is false)
            {
                return Result<LoginResponse, ErrorResponse>.Failure(ErrorResponse.Unauthorized("Unauthorized", "Wrong credentials"));
            }
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
            var tokens = await _tokenService.GenerateTokensAsync(user, userAgent, ipAddress,cancellationToken).ConfigureAwait(false);
            
            _cookieService.SetAuthCookies(httpContext, tokens);

            var rep = new LoginResponse(tokens.ExpirationAccessToken, tokens.ExpirationRefreshToken);
            return Result<LoginResponse, ErrorResponse>.Success(rep);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the login request for email: {Email}", request.email);
            return Result<LoginResponse, ErrorResponse>.Failure(ErrorResponse.Internal(ex.Message));
        }
    }
}
