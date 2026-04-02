using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Enums.RelationShip;
using MediatR;
using server.Application.Interfaces;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Application.Features.TaskFeatures.SelfManagement.DeleteTask;

public class DeleteTaskHandler : BaseFeatureHandler, IRequestHandler<DeleteTaskCommand, Unit>
{
    public DeleteTaskHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await UnitOfWork.Set<ProjectTask>().FindAsync(request.TaskId, cancellationToken);
        if (task == null) throw new KeyNotFoundException($"Task {request.TaskId} not found");
        await EnsureCurrentUserCanDeleteTask(task, cancellationToken);

        task.SoftDelete();

        return Unit.Value;
    }

    private async Task EnsureCurrentUserCanDeleteTask(
        ProjectTask task,
        CancellationToken cancellationToken)
    {
        if (task.CreatorId == CurrentUserId)
        {
            return;
        }

        var currentWorkspaceMemberId = await WorkspaceContext.GetWorkspaceMemberIdAsync(cancellationToken);
        
        var parentId = task.ProjectFolderId ?? task.ProjectSpaceId ?? task.ProjectWorkspaceId;
        var parentType = task.ProjectFolderId.HasValue ? EntityLayerType.ProjectFolder :
                        task.ProjectSpaceId.HasValue ? EntityLayerType.ProjectSpace : 
                        EntityLayerType.ProjectWorkspace;

        var accessibleCurrentMemberIds = await GetAccessibleMemberIds(
            parentId,
            parentType,
            new List<Guid> { currentWorkspaceMemberId });

        if (accessibleCurrentMemberIds.Count == 0)
        {
            throw new ValidationException("You do not have permission to delete this task.");
        }
    }
}
