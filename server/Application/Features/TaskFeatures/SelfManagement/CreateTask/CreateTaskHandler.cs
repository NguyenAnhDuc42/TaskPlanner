using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.TaskFeatures.SelfManagement.CreateTask;

public class CreateTaskHandler : BaseCommandHandler, IRequestHandler<CreateTaskCommand, Guid>
{
    public CreateTaskHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Guid> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var list = await FindOrThrowAsync<ProjectList>(request.ListId) as ProjectList
            ?? throw new KeyNotFoundException("List not found");

        await RequirePermissionAsync(list, EntityType.ProjectTask, PermissionAction.Create, cancellationToken);

        // Get or use default status (TODO: implement GetDefaultStatusId when Status CRUD is done)
        Guid statusId = request.StatusId ?? Guid.Empty; // Temporary - will need actual status

        var task = ProjectTask.Create(
            projectListId: request.ListId,
            name: request.Name,
            description: request.Description,
            customization: null,
            creatorId: CurrentUserId,
            statusId: statusId,
            priority: request.Priority,
            orderKey: list.GetNextTaskOrderAndIncrement(),
            startDate: request.StartDate,
            dueDate: request.DueDate,
            storyPoints: request.StoryPoints,
            timeEstimate: request.TimeEstimate
        );

        await UnitOfWork.Set<ProjectTask>().AddAsync(task, cancellationToken);

        // Immediate assignment
        if (request.AssigneeIds?.Any() == true)
        {
            // Validate assignees are workspace members
            var validMembers = await ValidateWorkspaceMembers(request.AssigneeIds, cancellationToken);

            // Create assignments
            var assignments = validMembers
                .Select(userId => TaskAssignment.Assign(task.Id, userId, CurrentUserId));

            await UnitOfWork.Set<TaskAssignment>().AddRangeAsync(assignments, cancellationToken);
        }

        return task.Id;
    }
}
