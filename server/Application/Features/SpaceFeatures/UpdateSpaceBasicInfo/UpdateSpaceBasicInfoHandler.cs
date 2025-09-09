using System;
using Application.Contract.SpaceContract;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Enums;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using server.Application.Interfaces;

namespace Application.Features.SpaceFeatures.UpdateSpaceBasicInfo;

public class UpdateSpaceBasicInfoHandler : IRequestHandler<UpdateSpaceBasicInfoCommand, SpaceSummary>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<UpdateSpaceBasicInfoHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    public UpdateSpaceBasicInfoHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ILogger<UpdateSpaceBasicInfoHandler> logger, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _permissionService = permissionService;
        _logger = logger;
        _currentUserService = currentUserService;
    }
    public async Task<SpaceSummary> Handle(UpdateSpaceBasicInfoCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        var space = await _unitOfWork.ProjectSpaces.GetByIdAsync(request.spaceId, cancellationToken);

        await _permissionService.EnsurePermissionAsync(currentUserId, space.ProjectWorkspaceId, Permission.Edit_Spaces, cancellationToken);

        space.UpdateBasicInfo(request.name, request.description);
        _unitOfWork.ProjectSpaces.Update(space);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return space.Adapt<SpaceSummary>();

    }
}
