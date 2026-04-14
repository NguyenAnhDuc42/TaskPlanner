using Application.Common.Results;
using Application.Common.Interfaces;
using Application.Helpers;
using Application.Interfaces.Data;
using Application.Features;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TaskFeatures.Helpers.GetTasksByUser;

public class GetTasksByUserHandler(IDataBase db) : IQueryHandler<GetTasksByUserQuery, List<TaskSummaryDto>>
{
    public async Task<Result<List<TaskSummaryDto>>> Handle(GetTasksByUserQuery request, CancellationToken ct)
    {
        var results = await (from ta in db.TaskAssignments
                            join t in db.Tasks on ta.ProjectTaskId equals t.Id
                            where ta.WorkspaceMemberId == request.MemberId
                                  && !t.DeletedAt.HasValue
                            select new TaskSummaryDto(t.Id, t.Name))
                           .AsNoTracking()
                           .ToListAsync(ct);

        return Result<List<TaskSummaryDto>>.Success(results);
    }
}
