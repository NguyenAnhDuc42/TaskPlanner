using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Enums.RelationShip;
using MediatR;
using server.Application.Interfaces;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Application.Features.TaskFeatures.AssigneeManagement.GetTaskAssignees;

public class GetTaskAssigneesHandler : BaseFeatureHandler, IRequestHandler<GetTaskAssigneesQuery, List<TaskAssigneeDto>>
{
    public GetTaskAssigneesHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<List<TaskAssigneeDto>> Handle(GetTaskAssigneesQuery request, CancellationToken cancellationToken)
    {
        var task = await UnitOfWork.Set<ProjectTask>().FindAsync(request.TaskId, cancellationToken);
        if (task == null) throw new KeyNotFoundException($"Task {request.TaskId} not found");
        await EnsureCurrentUserCanAccessTask(task, cancellationToken);

        var assignees = await UnitOfWork.QueryAsync<TaskAssigneeDto>(@"
            SELECT u.id AS UserId, u.name AS UserName, NULL AS AvatarUrl
            FROM task_assignments ta
            JOIN workspace_members wm ON ta.workspace_member_id = wm.id
            JOIN users u ON wm.user_id = u.id
            WHERE ta.task_id = @TaskId
              AND ta.deleted_at IS NULL
              AND wm.deleted_at IS NULL
            ORDER BY u.name", new { TaskId = task.Id }, cancellationToken);

        return assignees.ToList();
    }

    private async Task EnsureCurrentUserCanAccessTask(ProjectTask task, CancellationToken cancellationToken)
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
            throw new ValidationException("You do not have permission to view this task assignee list.");
        }
    }
}
