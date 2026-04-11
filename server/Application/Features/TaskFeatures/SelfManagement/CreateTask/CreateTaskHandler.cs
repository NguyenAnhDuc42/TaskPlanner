using Application.Helpers;
using Application.Interfaces.Data;
using Application.Common.Results;
using Application.Common.Errors;
using Domain.Common;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Domain.Entities.ProjectEntities.ValueObject;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using Dapper;

namespace Application.Features.TaskFeatures.SelfManagement.CreateTask;

public class CreateTaskHandler : ICommandHandler<CreateTaskCommand, TaskDto>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;

    public CreateTaskHandler(IDataBase db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<TaskDto>> Handle(CreateTaskCommand request, CancellationToken ct)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result.Failure<TaskDto>(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        // 1. Resolve Ancestors
        var ancestors = await HierarchyHelper.GetAncestorChain(_db, request.ParentId, request.ParentType, ct);
        
        // 2. Resolve OrderKey
        string orderKey = request.ParentType switch
        {
            EntityLayerType.ProjectFolder => await ResolveFolderOrderKey(request.ParentId, ct),
            EntityLayerType.ProjectSpace => await ResolveSpaceOrderKey(request.ParentId, ct),
            _ => FractionalIndex.Start()
        };

        // 3. Resolve Status
        var statusId = await ResolveStatusId(ancestors.ProjectWorkspaceId, request.StatusId);

        // 4. Create Task
        var slug = SlugHelper.GenerateSlug(request.Name);
        var task = ProjectTask.Create(
            ancestors.ProjectWorkspaceId,
            ancestors.ProjectSpaceId,
            ancestors.ProjectFolderId,
            request.Name,
            slug,
            request.Description,
            null,
            currentUserId,
            statusId,
            request.Priority,
            orderKey,
            request.StartDate,
            request.DueDate,
            request.StoryPoints,
            request.TimeEstimate
        );

        await _db.Tasks.AddAsync(task, ct);

        // 5. Assignments
        var assignees = new List<AssigneeDto>();
        if (request.AssigneeIds?.Any() == true)
        {
            assignees = await HandleAssignments(task, ancestors.ProjectWorkspaceId, request.AssigneeIds, currentUserId, ct);
        }

        await _db.SaveChangesAsync(ct);

        return Result.Success(new TaskDto(
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
        var maxKey = await _db.Tasks.ByFolder(folderId).WhereNotDeleted().MaxAsync(t => (string?)t.OrderKey, ct);
        return maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
    }

    private async Task<string> ResolveSpaceOrderKey(Guid spaceId, CancellationToken ct)
    {
        var maxKey = await _db.Tasks.BySpace(spaceId).Where(t => t.ProjectFolderId == null).WhereNotDeleted().MaxAsync(t => (string?)t.OrderKey, ct);
        return maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
    }

    private async Task<Guid?> ResolveStatusId(Guid workspaceId, Guid? requestedStatusId)
    {
        if (requestedStatusId.HasValue)
        {
            var isValid = await _db.Connection.QuerySingleOrDefaultAsync<int>(@"
                SELECT COUNT(1) FROM statuses s JOIN workflows w ON s.workflow_id = w.id
                WHERE s.id = @Id AND w.project_workspace_id = @WorkspaceId AND s.deleted_at IS NULL", 
                new { Id = requestedStatusId.Value, WorkspaceId = workspaceId });
            if (isValid > 0) return requestedStatusId;
        }

        return await _db.Connection.QuerySingleOrDefaultAsync<Guid?>(@"
            SELECT s.id FROM statuses s JOIN workflows w ON s.workflow_id = w.id
            WHERE w.project_workspace_id = @WorkspaceId AND s.deleted_at IS NULL
            ORDER BY s.created_at ASC LIMIT 1", new { WorkspaceId = workspaceId });
    }

    private async Task<List<AssigneeDto>> HandleAssignments(ProjectTask task, Guid workspaceId, List<Guid> userIds, Guid currentUserId, CancellationToken ct)
    {
        var memberIds = await _db.WorkspaceMembers
            .Where(wm => wm.ProjectWorkspaceId == workspaceId && userIds.Contains(wm.UserId))
            .Select(wm => wm.Id)
            .ToListAsync(ct);

        var assignments = memberIds.Select(memberId => TaskAssignment.Create(task.Id, memberId, currentUserId)).ToList();
        task.AddAsignees(assignments);

        var details = await _db.Connection.QueryAsync<AssigneeDto>(@"
            SELECT u.id AS Id, u.name AS Name, NULL AS AvatarUrl
            FROM users u JOIN workspace_members wm ON wm.user_id = u.id
            WHERE wm.id = ANY(@MemberIds)", new { MemberIds = memberIds.ToArray() });
        return details.ToList();
    }
}
