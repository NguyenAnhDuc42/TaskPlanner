using System;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using src.Domain.Entities.UserEntity;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Abstractions.IServices;

namespace src.Feature.User.Auth.Me;

public class MeRequestHandler : IRequestHandler<MeRequest, Result<MeResponse, ErrorResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<MeRequestHandler> _logger;

    public MeRequestHandler(
        IUserRepository userRepository,
        ITokenService tokenService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<MeRequestHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<MeResponse, ErrorResponse>> Handle(MeRequest request, CancellationToken cancellationToken)
    {

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            _logger.LogError("Unable to get HttpContext from IHttpContextAccessor.");
            return Result<MeResponse, ErrorResponse>.Failure(ErrorResponse.Internal("An unexpected error occurred."));
        }

        // Get user from JWT token
        var user = httpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return Result<MeResponse, ErrorResponse>.Failure(ErrorResponse.Unauthorized("Unauthorized", "User is not authenticated"));
        }

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Result<MeResponse, ErrorResponse>.Failure(ErrorResponse.Unauthorized("Unauthorized", "Invalid token"));
        }

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Result<MeResponse, ErrorResponse>.Failure(ErrorResponse.Unauthorized("Unauthorized", "Invalid user ID in token"));
        }
        var userEntity = await _userRepository.GetUserByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userEntity is null)
        {
            return Result<MeResponse, ErrorResponse>.Failure(ErrorResponse.NotFound("User not found", "User does not exist"));
        }

        var response = new MeResponse(
            userEntity.Id.ToString(),
            userEntity.Name,
            userEntity.Email.Value,
            "avatar"
        );
        return Result<MeResponse, ErrorResponse>.Success(response);

    }
}