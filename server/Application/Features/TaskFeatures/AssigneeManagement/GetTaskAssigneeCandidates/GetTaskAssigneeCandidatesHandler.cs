using Application.Common.Errors;
using Application.Common.Results;
using Domain.Entities;
using Application.Interfaces;
using Application.Interfaces.Data;
using Dapper;

namespace Application.Features.TaskFeatures;

public class GetTaskAssigneeCandidatesHandler(IDataBase db)
    : IQueryHandler<GetTaskAssigneeCandidatesQuery, List<TaskAssigneeCandidateDto>>
{
    public async Task<Result<List<TaskAssigneeCandidateDto>>> Handle(GetTaskAssigneeCandidatesQuery request, CancellationToken ct)
    {
        var task = await db.Tasks.FindAsync(request.TaskId, ct);
        if (task == null) return TaskError.NotFound;

        var assignedUserIds = (await db.Connection.QueryAsync<Guid>(
            GetTaskAssigneeCandidatesSQL.GetAssignedUserIds, 
            new { TaskId = task.Id })).ToArray();

        var safeLimit = request.Limit <= 0 ? 50 : Math.Min(request.Limit, 100);

        var candidates = await db.Connection.QueryAsync<TaskAssigneeCandidateDto>(
            GetTaskAssigneeCandidatesSQL.GetCandidates, 
            new
            {
                WorkspaceId = task.ProjectWorkspaceId,
                Search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
                AssignedUserIds = assignedUserIds,
                Limit = safeLimit
            });

        return candidates.ToList();
    }
}
