using Application.Interfaces.Data;
using Application.Common.Results;
using Application.Common.Errors;
using Domain.Entities;
using Dapper;

namespace Application.Features.TaskFeatures;

public class GetTaskListAssigneesHandler(IDataBase db) : IQueryHandler<GetTaskListAssigneesQuery, List<TaskAssigneeOptionDto>>
{
    public async Task<Result<List<TaskAssigneeOptionDto>>> Handle(GetTaskListAssigneesQuery request, CancellationToken ct)
    {
        var folder = await db.Folders.FindAsync(request.ListId, ct);
        if (folder == null) return FolderError.NotFound;

        var members = await db.Connection.QueryAsync<TaskAssigneeOptionDto>(
            GetTaskListAssigneesSQL.GetWorkspaceMembers, 
            new { WorkspaceId = folder.ProjectWorkspaceId });

        return members.ToList();
    }
}
