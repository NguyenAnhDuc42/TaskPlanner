using System;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using src.Application.Common.DTOs;
using src.Domain.Entities.UserEntity;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Abstractions.IServices;

namespace src.Feature.User.Auth.Me;

public class MeRequestHandler : IRequestHandler<MeRequest, Result<UserDetail, ErrorResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<MeRequestHandler> _logger;

    public MeRequestHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<MeRequestHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<UserDetail, ErrorResponse>> Handle(MeRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.CurrentUserId();
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("Could not retrieve current user ID. User may not be authenticated.");
            return Result<UserDetail, ErrorResponse>.Failure(ErrorResponse.Unauthorized());
        }

        var userEntity = await _unitOfWork.Users.GetUserByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userEntity is null)
        {
            return Result<UserDetail, ErrorResponse>.Failure(ErrorResponse.NotFound("User not found", "User does not exist"));
        }

        var response = new UserDetail(
            userEntity.Id,
            userEntity.Name,
            userEntity.Email
        );
        return Result<UserDetail, ErrorResponse>.Success(response);

    }
}