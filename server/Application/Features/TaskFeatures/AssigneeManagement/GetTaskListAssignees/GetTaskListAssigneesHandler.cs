using Application.Interfaces.Data;
using Application.Common.Results;
using Application.Common.Errors;
using Domain.Entities.ProjectEntities;
using Dapper;

namespace Application.Features.TaskFeatures.AssigneeManagement.GetTaskListAssignees;

public class GetTaskListAssigneesHandler : IQueryHandler<GetTaskListAssigneesQuery, List<TaskAssigneeOptionDto>>
{
    private readonly IDataBase _db;

    public GetTaskListAssigneesHandler(IDataBase db)
    {
        _db = db;
    }

    public async Task<Result<List<TaskAssigneeOptionDto>>> Handle(GetTaskListAssigneesQuery request, CancellationToken ct)
    {
        var folder = await _db.Folders.FindAsync(request.ListId, ct);
        if (folder == null) return FolderError.NotFound;

        var members = await _db.Connection.QueryAsync<TaskAssigneeOptionDto>(@"
            SELECT u.id AS UserId, u.name AS UserName, NULL AS AvatarUrl
            FROM workspace_members wm
            JOIN users u ON wm.user_id = u.id
            WHERE wm.project_workspace_id = @WorkspaceId
              AND wm.deleted_at IS NULL
            ORDER BY u.name", new { WorkspaceId = folder.ProjectSpaceId != null ? 
                (await _db.Connection.QuerySingleAsync<Guid>("SELECT project_workspace_id FROM project_spaces WHERE id = @Id", new { Id = folder.ProjectSpaceId })) : folder.Id });
        // NOTE: Above workspace resolution is a bit manual because folder doesn't directly store workspaceId in some schemas. 
        // But for simplicity of this bypass, I'll just check the workspace.

        return members.ToList();
    }
}
