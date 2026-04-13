using Application.Common.Errors;
using Application.Common.Results;
using Domain.Entities.ProjectEntities;
using Application.Interfaces;
using Application.Interfaces.Data;
using Dapper;

namespace Application.Features.TaskFeatures.AssigneeManagement.GetTaskAssigneeCandidates;

public class GetTaskAssigneeCandidatesHandler
    : IQueryHandler<GetTaskAssigneeCandidatesQuery, List<TaskAssigneeCandidateDto>>
{
    private readonly IDataBase _db;

    public GetTaskAssigneeCandidatesHandler(IDataBase db)
    {
        _db = db;
    }

    public async Task<Result<List<TaskAssigneeCandidateDto>>> Handle(
        GetTaskAssigneeCandidatesQuery request,
        CancellationToken ct)
    {
        var task = await _db.Tasks.FindAsync(request.TaskId, ct);
        if (task == null) return TaskError.NotFound;

        var assignedUserIds = (await _db.Connection.QueryAsync<Guid>(@"
            SELECT wm.user_id
            FROM task_assignments ta
            JOIN workspace_members wm ON ta.workspace_member_id = wm.id
            WHERE ta.task_id = @TaskId
              AND ta.deleted_at IS NULL
              AND wm.deleted_at IS NULL", new { TaskId = task.Id })).ToArray();

        var safeLimit = request.Limit <= 0 ? 50 : Math.Min(request.Limit, 100);

        var candidates = await _db.Connection.QueryAsync<TaskAssigneeCandidateDto>(@"
            SELECT u.id AS UserId, u.name AS UserName, NULL AS AvatarUrl
            FROM workspace_members wm
            JOIN users u ON wm.user_id = u.id
            WHERE wm.project_workspace_id = @WorkspaceId
              AND wm.deleted_at IS NULL
              AND (@Search IS NULL OR u.name ILIKE ('%' || @Search || '%'))
              AND (array_length(@AssignedUserIds, 1) IS NULL OR NOT (u.id = ANY(@AssignedUserIds)))
            ORDER BY u.name
            LIMIT @Limit", new
        {
            WorkspaceId = task.ProjectWorkspaceId,
            Search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
            AssignedUserIds = assignedUserIds,
            Limit = safeLimit
        });

        return candidates.ToList();
    }
}
