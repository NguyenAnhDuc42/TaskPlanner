using Application.Helpers;
using Application.Interfaces.Data;
using Application.Common.Results;
using Application.Common.Errors;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Dapper;

namespace Application.Features.TaskFeatures;

public class UpdateTaskHandler(IDataBase db, WorkspaceContext context, IRealtimeService realtime) : ICommandHandler<UpdateTaskCommand>
{
    public async Task<Result> Handle(UpdateTaskCommand request, CancellationToken ct)
    {
        var task = await db.Tasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, ct);

        if (task == null) return Result.Failure(TaskError.NotFound);

        if (context.CurrentMember.Role > Role.Admin && task.CreatorId != context.CurrentMember.Id)
            return Result.Failure(MemberError.DontHavePermission);

        ApplyBasicDetails(task, request);
        await ApplyStatusUpdate(task, request, ct);
        ApplyPriorityUpdate(task, request);
        ApplyDateUpdate(task, request);
        ApplyEstimationUpdate(task, request);
        await ApplyAssigneeUpdate(task, request, ct);

        await db.SaveChangesAsync(ct);

        await realtime.NotifyWorkspaceAsync(context.workspaceId, "TaskUpdated", new { TaskId = task.Id, WorkspaceId = context.workspaceId }, ct);

        return Result.Success();
    }

    private static void ApplyBasicDetails(ProjectTask task, UpdateTaskCommand request)
    {
        if (request.Name != null)
        {
            task.UpdateName(request.Name);
            task.UpdateSlug(SlugHelper.GenerateSlug(request.Name));
        }
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
        if (request.StartDate.HasValue) task.UpdateStartDate(request.StartDate.Value);
        if (request.DueDate.HasValue) task.UpdateDueDate(request.DueDate.Value);
    }

    private static void ApplyEstimationUpdate(ProjectTask task, UpdateTaskCommand request)
    {
        if (request.StoryPoints.HasValue) task.UpdateStoryPoints(request.StoryPoints.Value);
        if (request.TimeEstimate.HasValue) task.UpdateTimeEstimate(request.TimeEstimate.Value);
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
            .Select(id => TaskAssignment.Create(task.Id, id, context.CurrentMember.Id))
            .ToList();
        task.AddAsignees(toAdd);
    }
}
