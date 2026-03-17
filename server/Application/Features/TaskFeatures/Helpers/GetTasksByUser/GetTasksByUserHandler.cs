using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Microsoft.EntityFrameworkCore;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.TaskFeatures.Helpers.GetTasksByUser;

public class GetTasksByUserHandler : BaseFeatureHandler, IRequestHandler<GetTasksByUserQuery, List<AssignedTaskDto>>
{
    public GetTasksByUserHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
    : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<List<AssignedTaskDto>> Handle(GetTasksByUserQuery request, CancellationToken cancellationToken)
    {
        var results =
        await (from ta in UnitOfWork.Set<TaskAssignment>()
               join t in UnitOfWork.Set<ProjectTask>() on ta.ProjectTaskId equals t.Id
               where ta.WorkspaceMemberId == request.workspaceMemberId
               && !t.DeletedAt.HasValue
               select new AssignedTaskDto(t.Id, t.Name))
               .AsNoTracking()
               .ToListAsync(cancellationToken);

        return results;
    }
}
