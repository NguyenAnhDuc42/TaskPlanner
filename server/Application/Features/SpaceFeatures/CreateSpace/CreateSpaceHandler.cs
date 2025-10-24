using System;
using Application.Contract.SpaceContract;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.Services.Permissions;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using server.Application.Interfaces;

namespace Application.Features.SpaceFeatures.CreateSpace;

public class CreateSpaceHandler : IRequestHandler<CreateSpaceCommand, SpaceSummary>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<CreateSpaceHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    public CreateSpaceHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ILogger<CreateSpaceHandler> logger, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _permissionService = permissionService;
        _logger = logger;
        _currentUserService = currentUserService;
    }
    public async Task<SpaceSummary> Handle(CreateSpaceCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        await _permissionService.EnsurePermissionAsync(currentUserId, request.workspaceId, Permission.Create_Spaces, cancellationToken);

        var space = ProjectSpace.Create(request.workspaceId, request.name, request.description, request.color, request.icon, request.visibility ?? Visibility.Public, currentUserId, request.orderKey);
        await _unitOfWork.ProjectSpaces.AddAsync(space, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Space {SpaceId} created by {UserId}", space.Id, currentUserId);
        return space.Adapt<SpaceSummary>();
    }
}
