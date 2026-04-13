using Application.Helpers;
using Application.Interfaces.Data;
using Application.Common.Results;
using Application.Common.Errors;
using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Domain.Entities.ProjectEntities.ValueObject;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Dapper;

namespace Application.Features.TaskFeatures.SelfManagement.CreateTask;

public class CreateTaskHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<CreateTaskCommand, TaskDto>
{
    public async Task<Result<TaskDto>> Handle(CreateTaskCommand request, CancellationToken ct)
    {
        // AUTHORIZATION: Only Member or above can create tasks
        if (context.CurrentMember.Role > Role.Member)
            return Result<TaskDto>.Failure(MemberError.DontHavePermission);

        var ancestors = await HierarchyHelper.GetAncestorChain(db, request.ParentId, request.ParentType, ct);

        string orderKey = request.ParentType switch
        {
            EntityLayerType.ProjectFolder => await ResolveFolderOrderKey(request.ParentId, ct),
            EntityLayerType.ProjectSpace => await ResolveSpaceOrderKey(request.ParentId, ct),
            _ => FractionalIndex.Start()
        };

        var statusId = await ResolveStatusId(ancestors.ProjectWorkspaceId, request.StatusId);

        var slug = SlugHelper.GenerateSlug(request.Name);
        var task = ProjectTask.Create(
            projectWorkspaceId: ancestors.ProjectWorkspaceId,
            projectSpaceId: ancestors.ProjectSpaceId,
            projectFolderId: ancestors.ProjectFolderId,
            name: request.Name,
            slug: slug,
            description: request.Description,
            customization: null,
            creatorId: context.CurrentMember.Id,
            statusId: statusId,
            priority: request.Priority,
            startDate: request.StartDate,
            dueDate: request.DueDate,
            storyPoints: request.StoryPoints,
            timeEstimate: request.TimeEstimate,
            orderKey: orderKey
        );

        await db.Tasks.AddAsync(task, ct);

        // Assignments
        var assignees = new List<AssigneeDto>();
        if (request.AssigneeIds?.Any() == true)
        {
            assignees = await HandleAssignments(task, ancestors.ProjectWorkspaceId, request.AssigneeIds, ct);
        }

        await db.SaveChangesAsync(ct);

        return Result<TaskDto>.Success(new TaskDto(
            task.Id,
            task.ProjectWorkspaceId,
            task.ProjectSpaceId,
            task.ProjectFolderId,
            task.Name,
            task.Description,
            task.StatusId,
            task.Priority,
            task.StartDate,
            task.DueDate,
            task.StoryPoints,
            task.TimeEstimate,
            task.OrderKey,
            task.CreatedAt,
            assignees
        ));
    }

    private async Task<string> ResolveFolderOrderKey(Guid folderId, CancellationToken ct)
    {
        var maxKey = await db.Tasks.ByFolder(folderId).WhereNotDeleted().MaxAsync(t => (string?)t.OrderKey, ct);
        return maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
    }

    private async Task<string> ResolveSpaceOrderKey(Guid spaceId, CancellationToken ct)
    {
        var maxKey = await db.Tasks.BySpace(spaceId).Where(t => t.ProjectFolderId == null).WhereNotDeleted().MaxAsync(t => (string?)t.OrderKey, ct);
        return maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
    }

    private async Task<Guid?> ResolveStatusId(Guid workspaceId, Guid? requestedStatusId)
    {
        if (requestedStatusId.HasValue)
        {
            var isValid = await db.Connection.QuerySingleOrDefaultAsync<int>(@"
                SELECT COUNT(1) FROM statuses s JOIN workflows w ON s.workflow_id = w.id
                WHERE s.id = @Id AND w.project_workspace_id = @WorkspaceId AND s.deleted_at IS NULL",
                new { Id = requestedStatusId.Value, WorkspaceId = workspaceId });
            if (isValid > 0) return requestedStatusId;
        }

        return await db.Connection.QuerySingleOrDefaultAsync<Guid?>(@"
            SELECT s.id FROM statuses s JOIN workflows w ON s.workflow_id = w.id
            WHERE w.project_workspace_id = @WorkspaceId AND s.deleted_at IS NULL
            ORDER BY s.created_at ASC LIMIT 1", new { WorkspaceId = workspaceId });
    }

    private async Task<List<AssigneeDto>> HandleAssignments(ProjectTask task, Guid workspaceId, List<Guid> userIds, CancellationToken ct)
    {
        var memberIds = await db.WorkspaceMembers
            .Where(wm => wm.ProjectWorkspaceId == workspaceId && userIds.Contains(wm.UserId))
            .Select(wm => wm.Id)
            .ToListAsync(ct);

        var assignments = memberIds.Select(memberId => TaskAssignment.Create(task.Id, memberId, context.CurrentMember.Id)).ToList();
        task.AddAsignees(assignments);

        var details = await db.Connection.QueryAsync<AssigneeDto>(@"
            SELECT u.id AS Id, u.name AS Name, NULL AS AvatarUrl
            FROM users u JOIN workspace_members wm ON wm.user_id = u.id
            WHERE wm.id = ANY(@MemberIds)", new { MemberIds = memberIds.ToArray() });
        return details.ToList();
    }
}
