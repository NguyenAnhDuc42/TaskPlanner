using Application.Interfaces.Repositories;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using MediatR;
using server.Application.Interfaces;
using Application.Contract.Common;
using Microsoft.EntityFrameworkCore;
using Domain.Entities.Relationship;
using Domain.Enums.RelationShip;
using Application.Features.TaskFeatures.Logic;
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
        var accessibleCurrentMemberIds = await GetAccessibleMemberIds(
            task.ProjectListId,
            EntityLayerType.ProjectList,
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
     
        var (effectiveLayerId, effectiveLayerType) =
            await TaskStatusLayerResolver.GetEffectiveStatusLayer(
                UnitOfWork,
                task.ProjectListId,
                EntityLayerType.ProjectList,
                cancellationToken);

        var mappedRequestedStatusId = await TaskStatusSemanticMapper.MapRequestedStatusToEffectiveLayer(
            UnitOfWork,
            effectiveLayerId,
            effectiveLayerType,
            request.StatusId,
            cancellationToken);

        var resolvedStatusId = await TaskStatusLayerResolver.ResolveTaskStatusId(
            UnitOfWork,
            task.ProjectListId,
            mappedRequestedStatusId,
            cancellationToken);

        if (!resolvedStatusId.HasValue) throw new ValidationException("No valid status found in effective status layer.");

        task.UpdateStatus(resolvedStatusId.Value);
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
        var accessibleMemberIds = await GetAccessibleMemberIds(
            task.ProjectListId,
            EntityLayerType.ProjectList,
            memberIds);

        if (accessibleMemberIds.Count != memberIds.Count) throw new ValidationException("One or more assignees do not have permission to access this List.");

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
            ProjectListId = task.ProjectListId,
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
