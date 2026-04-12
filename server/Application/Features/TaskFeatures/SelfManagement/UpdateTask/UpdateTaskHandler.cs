using Application.Helpers;
using Application.Interfaces.Data;
using Application.Common.Results;
using Application.Common.Errors;
using Application.Common.Interfaces;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using Dapper;

namespace Application.Features.TaskFeatures.SelfManagement.UpdateTask;

public class UpdateTaskHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<UpdateTaskCommand, TaskDto>
{
    public async Task<Result<TaskDto>> Handle(UpdateTaskCommand request, CancellationToken ct)
    {
        var task = await db.Tasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, ct);

        if (task == null) return Result<TaskDto>.Failure(TaskError.NotFound);

        // Permission: Admin/Owner or the task creator
        if (context.CurrentMember.Role > Role.Admin && task.CreatorId != context.CurrentMember.UserId)
            return Result<TaskDto>.Failure(MemberError.DontHavePermission);

        ApplyBasicDetails(task, request);
        await ApplyStatusUpdate(task, request, ct);
        ApplyPriorityUpdate(task, request);
        ApplyDateUpdate(task, request);
        ApplyEstimationUpdate(task, request);
        await ApplyAssigneeUpdate(task, request, ct);

        await db.SaveChangesAsync(ct);
        return await BuildTaskDto(task, ct);
    }

    private static void ApplyBasicDetails(ProjectTask task, UpdateTaskCommand request)
    {
        if (request.Name == null && request.Description == null) return;
        var slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null;
        task.UpdateBasicInfo(request.Name, slug, request.Description);
    }

    private async Task ApplyStatusUpdate(ProjectTask task, UpdateTaskCommand request, CancellationToken ct)
    {
        if (!request.StatusId.HasValue || request.StatusId.Value == task.StatusId) return;

        var isValid = await db.Connection.QuerySingleOrDefaultAsync<int>(@"
            SELECT COUNT(1) 
            FROM   statuses s
            JOIN   workflows w ON s.workflow_id = w.id
            WHERE  s.id = @Id 
              AND  w.project_workspace_id = @WorkspaceId 
              AND  s.deleted_at IS NULL", new { Id = request.StatusId.Value, WorkspaceId = task.ProjectWorkspaceId });

        if (isValid > 0) task.UpdateStatus(request.StatusId.Value);
    }

    private static void ApplyPriorityUpdate(ProjectTask task, UpdateTaskCommand request)
    {
        if (request.Priority.HasValue) task.UpdatePriority(request.Priority.Value);
    }

    private static void ApplyDateUpdate(ProjectTask task, UpdateTaskCommand request)
    {
        if (request.StartDate.HasValue || request.DueDate.HasValue)
            task.UpdateDates(request.StartDate ?? task.StartDate, request.DueDate ?? task.DueDate);
    }

    private static void ApplyEstimationUpdate(ProjectTask task, UpdateTaskCommand request)
    {
        if (request.StoryPoints.HasValue || request.TimeEstimate.HasValue)
            task.UpdateEstimation(request.StoryPoints ?? task.StoryPoints, request.TimeEstimate ?? task.TimeEstimate);
    }

    private async Task ApplyAssigneeUpdate(ProjectTask task, UpdateTaskCommand request, CancellationToken ct)
    {
        if (request.AssigneeIds == null) return;

        var memberIds = await db.WorkspaceMembers
            .Where(wm => wm.ProjectWorkspaceId == task.ProjectWorkspaceId && request.AssigneeIds.Contains(wm.UserId))
            .Select(wm => wm.Id)
            .ToListAsync(ct);

        var currentMemberIds = task.Assignees.Select(a => a.WorkspaceMemberId).ToHashSet();

        var toRemove = task.Assignees.Where(a => !memberIds.Contains(a.WorkspaceMemberId)).ToList();
        task.RemoveAsignees(toRemove.Select(a => a.WorkspaceMemberId).ToList());

        var toAdd = memberIds
            .Where(id => !currentMemberIds.Contains(id))
            .Select(id => TaskAssignment.Create(task.Id, id, context.CurrentMember.UserId))
            .ToList();
        task.AddAsignees(toAdd);
    }

    private async Task<Result<TaskDto>> BuildTaskDto(ProjectTask task, CancellationToken ct)
    {
        var assigneeDtos = await db.Connection.QueryAsync<AssigneeDto>(@"
            SELECT u.id AS Id, u.name AS Name, NULL AS AvatarUrl
            FROM   task_assignments ta
            JOIN   workspace_members wm ON ta.workspace_member_id = wm.id
            JOIN   users u             ON wm.user_id = u.id
            WHERE  ta.task_id = @TaskId
              AND  ta.deleted_at IS NULL", new { TaskId = task.Id });

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
            assigneeDtos.ToList()
        ));
    }
}
