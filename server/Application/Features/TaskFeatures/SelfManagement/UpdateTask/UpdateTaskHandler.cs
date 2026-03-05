using Application.Interfaces.Repositories;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using MediatR;
using server.Application.Interfaces;
using Application.Contract.Common;
using Microsoft.EntityFrameworkCore;
using Domain.Entities.Relationship;
using Domain.Enums.RelationShip;
using System.ComponentModel.DataAnnotations;
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

        var currentWorkspaceMemberId = await WorkspaceContext.GetWorkspaceMemberIdAsync(cancellationToken);
        var accessibleCurrentMemberIds = await GetAccessibleMemberIds(
            task.ProjectListId,
            EntityLayerType.ProjectList,
            new List<Guid> { currentWorkspaceMemberId });

        if (accessibleCurrentMemberIds.Count == 0)
        {
            throw new ValidationException("You do not have permission to update this task.");
        }

        // Update basic properties
        if (request.Name != null || request.Description != null)
        {
            task.UpdateDetails(
                request.Name ?? task.Name,
                request.Description ?? task.Description
            );
        }

        // Update status
        if (request.StatusId.HasValue && request.StatusId.Value != task.StatusId)
        {
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
            if (!resolvedStatusId.HasValue)
            {
                throw new ValidationException("No valid status found in effective status layer.");
            }
            task.UpdateStatus(resolvedStatusId.Value);
        }

        // Update priority
        if (request.Priority.HasValue && request.Priority.Value != task.Priority)
        {
            task.UpdatePriority(request.Priority.Value);
        }

        // Update dates
        if (request.StartDate.HasValue || request.DueDate.HasValue)
        {
            task.UpdateDates(
                request.StartDate ?? task.StartDate,
                request.DueDate ?? task.DueDate
            );
        }

        // Update estimation
        if (request.StoryPoints.HasValue || request.TimeEstimate.HasValue)
        {
            task.UpdateEstimation(
                request.StoryPoints ?? task.StoryPoints,
                request.TimeEstimate ?? task.TimeEstimate
            );
        }

        // Update assignees
        if (request.AssigneeIds != null)
        {
            // 1. Validate are workspace members
            var validUserIds = await ValidateWorkspaceMembers(request.AssigneeIds, cancellationToken);
            var memberIds = await GetWorkspaceMemberIds(validUserIds, cancellationToken);

            // 2. Validate bubble-up access
            var accessibleMemberIds = await GetAccessibleMemberIds(task.ProjectListId, EntityLayerType.ProjectList, memberIds);

            if (accessibleMemberIds.Count != memberIds.Count)
            {
                throw new ValidationException("One or more assignees do not have permission to access this List.");
            }

            // 3. Process changes
            var currentMemberIds = task.Assignees.Select(a => a.WorkspaceMemberId).ToHashSet();
            
            // Removal
            var toRemove = currentMemberIds.Where(id => !accessibleMemberIds.Contains(id)).ToList();
            task.RemoveAsignees(toRemove);

            // Addition
            var toAdd = accessibleMemberIds.Where(id => !currentMemberIds.Contains(id))
                .Select(id => TaskAssignment.Create(task.Id, id, CurrentUserId))
                .ToList();
            task.AddAsignees(toAdd);
        }

        await UnitOfWork.SaveChangesAsync(cancellationToken);
        // Map and return
        var assigneeDtos = await UnitOfWork.QueryAsync<AssigneeDto>(@"
            SELECT u.id AS Id, u.name AS Name, NULL AS AvatarUrl
            FROM   task_assignments ta
            JOIN   workspace_members wm ON ta.workspace_member_id = wm.id
            JOIN   users u             ON wm.user_id = u.id
            WHERE  ta.task_id = @TaskId
              AND  ta.deleted_at IS NULL", new { TaskId = task.Id });

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
