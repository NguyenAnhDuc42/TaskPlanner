using Application.Interfaces.Repositories;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using MediatR;
using server.Application.Interfaces;
using Application.Features.ViewFeatures.GetViewData;
using Microsoft.EntityFrameworkCore;
using Domain.Entities.Relationship;
using Domain.Enums.RelationShip;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Application.Features.TaskFeatures.SelfManagement.UpdateTask;

public class UpdateTaskHandler : BaseFeatureHandler, IRequestHandler<UpdateTaskCommand, TaskDto>
{
    public UpdateTaskHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<TaskDto> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await UnitOfWork.Set<ProjectTask>()
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new KeyNotFoundException($"Task not found: {request.TaskId}");

        await EnsureCurrentUserCanModifyTask(task, "update", cancellationToken);
        ApplyBasicDetails(task, request);
        await ApplyStatusUpdate(task, request, cancellationToken);
        ApplyPriorityUpdate(task, request);
        ApplyDateUpdate(task, request);
        ApplyEstimationUpdate(task, request);
        await ApplyAssigneeUpdate(task, request, cancellationToken);

        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return await BuildTaskDto(task, cancellationToken);
    }

    private async Task EnsureCurrentUserCanModifyTask(ProjectTask task, string action, CancellationToken cancellationToken)
    {
        if (task.CreatorId == CurrentUserId) return;

        var currentWorkspaceMemberId = await WorkspaceContext.GetWorkspaceMemberIdAsync(cancellationToken);
        
        // Use the deepest available ParentId for access check
        var parentId = task.ProjectFolderId ?? task.ProjectSpaceId ?? task.ProjectWorkspaceId;
        var parentType = task.ProjectFolderId.HasValue ? EntityLayerType.ProjectFolder :
                        task.ProjectSpaceId.HasValue ? EntityLayerType.ProjectSpace : 
                        EntityLayerType.ProjectWorkspace;

        var accessibleCurrentMemberIds = await GetAccessibleMemberIds(
            parentId,
            parentType,
            new List<Guid> { currentWorkspaceMemberId });

        if (accessibleCurrentMemberIds.Count == 0) throw new ValidationException($"You do not have permission to {action} this task.");
    }

    private static void ApplyBasicDetails(ProjectTask task, UpdateTaskCommand request)
    {
        if (request.Name == null && request.Description == null) return;

        task.UpdateDetails(
            request.Name ?? task.Name,
            request.Description ?? task.Description);
    }

    private async Task ApplyStatusUpdate(ProjectTask task, UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        if (!request.StatusId.HasValue || request.StatusId.Value == task.StatusId) return;
     
        // Statuses are now owned by the Workspace (Liquid Workflow)
        const string sql = @"
            SELECT COUNT(1) 
            FROM   statuses s
            JOIN   workflows w ON s.workflow_id = w.id
            WHERE  s.id = @Id 
              AND  w.project_workspace_id = @WorkspaceId 
              AND  s.deleted_at IS NULL";

        var isValid = await UnitOfWork.QuerySingleOrDefaultAsync<int>(sql, new { Id = request.StatusId.Value, WorkspaceId = task.ProjectWorkspaceId }, cancellationToken);
        
        if (isValid == 0) throw new ValidationException("Selected status does not belong to the workspace's workflow.");

        task.UpdateStatus(request.StatusId.Value);
    }

    private static void ApplyPriorityUpdate(ProjectTask task, UpdateTaskCommand request)
    {
        if (!request.Priority.HasValue || request.Priority.Value == task.Priority) return;

        task.UpdatePriority(request.Priority.Value);
    }

    private static void ApplyDateUpdate(ProjectTask task, UpdateTaskCommand request)
    {
        if (!request.StartDate.HasValue && !request.DueDate.HasValue) return;

        task.UpdateDates(
            request.StartDate ?? task.StartDate,
            request.DueDate ?? task.DueDate);
    }

    private static void ApplyEstimationUpdate(ProjectTask task, UpdateTaskCommand request)
    {
        if (!request.StoryPoints.HasValue && !request.TimeEstimate.HasValue) return;

        task.UpdateEstimation(
            request.StoryPoints ?? task.StoryPoints,
            request.TimeEstimate ?? task.TimeEstimate);
    }

    private async Task ApplyAssigneeUpdate(
        ProjectTask task,
        UpdateTaskCommand request,
        CancellationToken cancellationToken)
    {
        if (request.AssigneeIds == null) return;

        var validUserIds = await ValidateWorkspaceMembers(request.AssigneeIds, cancellationToken);
        var memberIds = await GetWorkspaceMemberIds(validUserIds, cancellationToken);

        var parentId = task.ProjectFolderId ?? task.ProjectSpaceId ?? task.ProjectWorkspaceId;
        var parentType = task.ProjectFolderId.HasValue ? EntityLayerType.ProjectFolder :
                        task.ProjectSpaceId.HasValue ? EntityLayerType.ProjectSpace : 
                        EntityLayerType.ProjectWorkspace;

        var accessibleMemberIds = await GetAccessibleMemberIds(
            parentId,
            parentType,
            memberIds);

        if (accessibleMemberIds.Count != memberIds.Count) throw new ValidationException("One or more assignees do not have permission to access this container.");

        var currentMemberIds = task.Assignees.Select(a => a.WorkspaceMemberId).ToHashSet();
        var toRemove = currentMemberIds.Where(id => !accessibleMemberIds.Contains(id)).ToList();
        task.RemoveAsignees(toRemove);

        var toAdd = accessibleMemberIds
            .Where(id => !currentMemberIds.Contains(id))
            .Select(id => TaskAssignment.Create(task.Id, id, CurrentUserId))
            .ToList();
        task.AddAsignees(toAdd);
    }

    private async Task<TaskDto> BuildTaskDto(ProjectTask task, CancellationToken cancellationToken)
    {
        var assigneeDtos = await UnitOfWork.QueryAsync<AssigneeDto>(@"
            SELECT u.id AS Id, u.name AS Name, NULL AS AvatarUrl
            FROM   task_assignments ta
            JOIN   workspace_members wm ON ta.workspace_member_id = wm.id
            JOIN   users u             ON wm.user_id = u.id
            WHERE  ta.task_id = @TaskId
              AND  ta.deleted_at IS NULL", new { TaskId = task.Id }, cancellationToken);

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
