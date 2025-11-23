using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.TaskManagement.AssigneeManagement.UnassignUsers;

public class UnassignUsersFromTaskHandler : BaseCommandHandler, IRequestHandler<UnassignUsersFromTaskCommand, Unit>
{
    public UnassignUsersFromTaskHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UnassignUsersFromTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await FindOrThrowAsync<ProjectTask>(request.TaskId) as ProjectTask
            ?? throw new KeyNotFoundException("Task not found");

        await RequirePermissionAsync(task, PermissionAction.Edit, cancellationToken);

        // Remove assignments
        var assignmentsToRemove = await UnitOfWork.Set<TaskAssignment>()
            .Where(ta => ta.TaskId == task.Id && request.UserIds.Contains(ta.AssigneeId))
            .ToListAsync(cancellationToken);

        if (assignmentsToRemove.Any())
        {
            UnitOfWork.Set<TaskAssignment>().RemoveRange(assignmentsToRemove);
        }

        return Unit.Value;
    }
}
