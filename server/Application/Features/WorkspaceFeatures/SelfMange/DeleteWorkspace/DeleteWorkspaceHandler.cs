using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfMange.DeleteWorkspace;

public class DeleteWorkspaceHandler : IRequestHandler<DeleteWorkspaceCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    public DeleteWorkspaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPermissionService permissionService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    }

    public async Task<Guid> Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) throw new UnauthorizedAccessException("User not authenticated");
        var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(request.workspaceId, cancellationToken);
        if (workspace == null) throw new InvalidOperationException("Workspace not found");

        await _permissionService.EnsurePermissionAsync(currentUserId, request.workspaceId, Permission.Delete_Workspace, cancellationToken);
        _unitOfWork.ProjectWorkspaces.Remove(workspace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workspace.Id;

    }
}
