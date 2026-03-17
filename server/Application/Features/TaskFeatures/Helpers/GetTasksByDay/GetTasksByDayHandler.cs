
using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.TaskFeatures.Helpers.GetTasksByDay;

public class GetTaskByDayHandler : BaseFeatureHandler, IRequestHandler<GetTaskByDayQuery, List<WorkspaceGroupDto>>
{
    public GetTaskByDayHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
    : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<List<WorkspaceGroupDto>> Handle(GetTaskByDayQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = CurrentUserService.CurrentUserId();
        var dayStart = new DateTimeOffset(DateTime.SpecifyKind(request.day.Date, DateTimeKind.Utc));
        var dayEnd = dayStart.AddDays(1);

        var result =
            await (from wm in UnitOfWork.Set<WorkspaceMember>()
                   join w in UnitOfWork.Set<ProjectWorkspace>() on wm.ProjectWorkspaceId equals w.Id
                   join ta in UnitOfWork.Set<TaskAssignment>() on wm.Id equals ta.WorkspaceMemberId
                   join t in UnitOfWork.Set<ProjectTask>() on ta.ProjectTaskId equals t.Id
                   where wm.UserId == currentUserId
                   && !t.DeletedAt.HasValue
                   && (
                         (t.DueDate >= dayStart && t.DueDate < dayEnd)
                         || (t.StartDate.HasValue && t.DueDate.HasValue && t.StartDate.Value < dayEnd && t.DueDate.Value >= dayStart)
                         || (t.StartDate.HasValue && !t.DueDate.HasValue && t.StartDate.Value >= dayStart && t.StartDate.Value < dayEnd)
                     )
                   group new TaskSummaryDto(t.Id, t.ProjectListId, t.Name)
                   by new { w.Id, w.Name } into g
                   select new WorkspaceGroupDto(g.Key.Id, g.Key.Name, g.ToList()))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return result;
    }
}
