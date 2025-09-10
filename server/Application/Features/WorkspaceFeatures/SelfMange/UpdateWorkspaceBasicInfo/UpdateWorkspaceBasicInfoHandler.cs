using System;
using Application.Contract.WorkspaceContract;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using Mapster;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfMange.UpdateWorkspaceBasicInfo;

public class UpdateWorkspaceBasicInfoHandler : IRequestHandler<UpdateWorkspaceBasicInfoCommand, WorkspaceDetail>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;

    public UpdateWorkspaceBasicInfoHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPermissionService permissionService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    }

    public async Task<WorkspaceDetail> Handle(UpdateWorkspaceBasicInfoCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) throw new UnauthorizedAccessException("User not authenticated");
        var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(request.workspaceId, cancellationToken);
        if (workspace == null) throw new InvalidOperationException("Workspace not found");

        await _permissionService.EnsurePermissionAsync(currentUserId, request.workspaceId, Permission.Edit_Workspace, cancellationToken);
        var newName = request.name ?? workspace.Name;
        var newDescription = request.description ?? workspace.Description ?? string.Empty;
        workspace.UpdateBasicInfo(newName, newDescription);

        _unitOfWork.ProjectWorkspaces.Update(workspace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return workspace.Adapt<WorkspaceDetail>();
    }
}
