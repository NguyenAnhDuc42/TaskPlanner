using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfMange.DeleteWorkspace;

public class DeleteWorkspaceHandler : IRequestHandler<DeleteWorkspaceCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    public DeleteWorkspaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Guid> Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await _unitOfWork.Set<ProjectWorkspace>().FindAsync(request.workspaceId);
        if (workspace == null)
        {
            throw new KeyNotFoundException("Workspace not found");
        }

        if (workspace.CreatorId != _currentUserService.CurrentUserId())
        {
            throw new UnauthorizedAccessException("You are not authorized to delete this workspace");
        }

        _unitOfWork.Set<ProjectWorkspace>().Remove(workspace);
    
        return request.workspaceId;

    }
}
