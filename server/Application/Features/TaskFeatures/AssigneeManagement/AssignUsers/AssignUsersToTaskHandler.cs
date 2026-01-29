using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.TaskFeatures.AssigneeManagement.AssignUsers;

public class AssignUsersToTaskHandler : BaseCommandHandler, IRequestHandler<AssignUsersToTaskCommand, Unit>
{
    public AssignUsersToTaskHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(AssignUsersToTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await FindOrThrowAsync<ProjectTask>(request.TaskId) as ProjectTask
            ?? throw new KeyNotFoundException("Task not found");

        await RequirePermissionAsync(task, PermissionAction.Edit, cancellationToken);

        // Validate all users are workspace members
        var validMembers = await ValidateWorkspaceMembers(request.UserIds, cancellationToken);

        // Get existing assignments to prevent duplicates
        var existingAssignees = await UnitOfWork.Set<TaskAssignment>()
            .Where(ta => ta.TaskId == task.Id)
            .Select(ta => ta.AssigneeId)
            .ToListAsync(cancellationToken);

        // Filter out already assigned users
        var newAssignees = validMembers
            .Where(userId => !existingAssignees.Contains(userId))
            .ToList();

        if (newAssignees.Any())
        {
            var assignments = newAssignees
                .Select(userId => TaskAssignment.Assign(task.Id, userId, CurrentUserId));

            await UnitOfWork.Set<TaskAssignment>().AddRangeAsync(assignments, cancellationToken);
        }

        return Unit.Value;
    }
}
