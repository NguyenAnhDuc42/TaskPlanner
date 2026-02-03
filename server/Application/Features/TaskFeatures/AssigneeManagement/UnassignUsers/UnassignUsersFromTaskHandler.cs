using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.TaskFeatures.AssigneeManagement.UnassignUsers;

public class UnassignUsersFromTaskHandler : BaseFeatureHandler, IRequestHandler<UnassignUsersFromTaskCommand, Unit>
{
    public UnassignUsersFromTaskHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UnassignUsersFromTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await FindOrThrowAsync<ProjectTask>(request.TaskId);

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
