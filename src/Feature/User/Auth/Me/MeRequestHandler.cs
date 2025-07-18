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
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<MeRequestHandler> _logger;

    public MeRequestHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        ILogger<MeRequestHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<MeResponse, ErrorResponse>> Handle(MeRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.CurrentUserId();
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("Could not retrieve current user ID. User may not be authenticated.");
            return Result<MeResponse, ErrorResponse>.Failure(ErrorResponse.Unauthorized("Unauthorized", "User is not authenticated or token is invalid."));
        }

        var userEntity = await _userRepository.GetUserByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userEntity is null)
        {
            return Result<MeResponse, ErrorResponse>.Failure(ErrorResponse.NotFound("User not found", "User does not exist"));
        }

        var response = new MeResponse(
            userEntity.Id.ToString(),
            userEntity.Name,
            userEntity.Email,
            "avatar"
        );
        return Result<MeResponse, ErrorResponse>.Success(response);

    }
}