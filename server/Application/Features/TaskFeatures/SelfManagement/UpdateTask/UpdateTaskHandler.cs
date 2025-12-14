using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.TaskFeatures.SelfManagement.UpdateTask;

public class UpdateTaskHandler : BaseCommandHandler, IRequestHandler<UpdateTaskCommand, Unit>
{
    public UpdateTaskHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await FindOrThrowAsync<ProjectTask>(request.TaskId) as ProjectTask
            ?? throw new KeyNotFoundException("Task not found");

        await RequirePermissionAsync(task, PermissionAction.Edit, cancellationToken);

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
            task.UpdateStatus(request.StatusId.Value);
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

        return Unit.Value;
    }
}
