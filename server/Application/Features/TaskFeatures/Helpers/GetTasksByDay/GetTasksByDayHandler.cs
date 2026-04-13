using Application.Common.Results;
using Application.Common.Interfaces;
using Application.Helpers;
using Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Application.Features.TaskFeatures.Helpers.GetTasksByDay;

public class GetTasksByDayHandler(IDataBase db, WorkspaceContext context) : IQueryHandler<GetTasksByDayQuery, List<WorkspaceGroupDto>>
{
    public async Task<Result<List<WorkspaceGroupDto>>> Handle(GetTasksByDayQuery request, CancellationToken ct)
    {
        var currentUserId = context.CurrentMember.UserId;
        var dayStart = new DateTimeOffset(DateTime.SpecifyKind(request.day.Date, DateTimeKind.Utc));
        var dayEnd = dayStart.AddDays(1);

        var result = await (from wm in db.WorkspaceMembers.ByUser(currentUserId)
                           join w in db.Workspaces on wm.ProjectWorkspaceId equals w.Id
                           join ta in db.TaskAssignments on wm.Id equals ta.WorkspaceMemberId
                           join t in db.Tasks on ta.ProjectTaskId equals t.Id
                           where wm.UserId == currentUserId
                           && !t.DeletedAt.HasValue
                           && (
                                 (t.DueDate >= dayStart && t.DueDate < dayEnd)
                                 || (t.StartDate.HasValue && t.DueDate.HasValue && t.StartDate.Value < dayEnd && t.DueDate.Value >= dayStart)
                                 || (t.StartDate.HasValue && !t.DueDate.HasValue && t.StartDate.Value >= dayStart && t.StartDate.Value < dayEnd)
                             )
                           group new TaskSummaryDto(t.Id, t.Name)
                           by new { w.Id, w.Name } into g
                           select new WorkspaceGroupDto(g.Key.Id, g.Key.Name, g.ToList()))
                    .AsNoTracking()
                    .ToListAsync(ct);

        return Result<List<WorkspaceGroupDto>>.Success(result);
    }
}
