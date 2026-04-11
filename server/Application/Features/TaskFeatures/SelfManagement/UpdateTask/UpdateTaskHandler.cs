using Application.Helpers;
using Application.Interfaces.Data;
using Application.Common.Results;
using Application.Common.Errors;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using Dapper;

namespace Application.Features.TaskFeatures.SelfManagement.UpdateTask;

public class UpdateTaskHandler : ICommandHandler<UpdateTaskCommand, TaskDto>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;

    public UpdateTaskHandler(IDataBase db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<TaskDto>> Handle(UpdateTaskCommand request, CancellationToken ct)
    {
        var task = await _db.Tasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, ct);
        
        if (task == null) return TaskError.NotFound;

        ApplyBasicDetails(task, request);
        await ApplyStatusUpdate(task, request, ct);
        ApplyPriorityUpdate(task, request);
        ApplyDateUpdate(task, request);
        ApplyEstimationUpdate(task, request);
        await ApplyAssigneeUpdate(task, request, ct);

        await _db.SaveChangesAsync(ct);
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
        
        var isValid = await _db.Connection.QuerySingleOrDefaultAsync<int>(@"
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

        var currentUserId = _currentUserService.CurrentUserId();
        var memberIds = await _db.WorkspaceMembers
            .Where(wm => wm.ProjectWorkspaceId == task.ProjectWorkspaceId && request.AssigneeIds.Contains(wm.UserId))
            .Select(wm => wm.Id)
            .ToListAsync(ct);

        var currentMemberIds = task.Assignees.Select(a => a.WorkspaceMemberId).ToHashSet();
        
        var toRemove = task.Assignees.Where(a => !memberIds.Contains(a.WorkspaceMemberId)).ToList();
        task.RemoveAsignees(toRemove.Select(a => a.WorkspaceMemberId).ToList());

        var toAdd = memberIds
            .Where(id => !currentMemberIds.Contains(id))
            .Select(id => TaskAssignment.Create(task.Id, id, currentUserId))
            .ToList();
        task.AddAsignees(toAdd);
    }

    private async Task<TaskDto> BuildTaskDto(ProjectTask task, CancellationToken ct)
    {
        var assigneeDtos = await _db.Connection.QueryAsync<AssigneeDto>(@"
            SELECT u.id AS Id, u.name AS Name, NULL AS AvatarUrl
            FROM   task_assignments ta
            JOIN   workspace_members wm ON ta.workspace_member_id = wm.id
            JOIN   users u             ON wm.user_id = u.id
            WHERE  ta.task_id = @TaskId
              AND  ta.deleted_at IS NULL", new { TaskId = task.Id });

        return new TaskDto
        {
            Id = task.Id,
            ProjectWorkspaceId = task.ProjectWorkspaceId,
            ProjectSpaceId = task.ProjectSpaceId,
            ProjectFolderId = task.ProjectFolderId,
            Name = task.Name,
            Description = task.Description,
            StatusId = task.StatusId,
            Priority = task.Priority,
            StartDate = task.StartDate,
            DueDate = task.DueDate,
            StoryPoints = task.StoryPoints,
            TimeEstimate = task.TimeEstimate,
            OrderKey = task.OrderKey,
            CreatedAt = task.CreatedAt,
            Assignees = assigneeDtos.ToList()
        };
    }
}
