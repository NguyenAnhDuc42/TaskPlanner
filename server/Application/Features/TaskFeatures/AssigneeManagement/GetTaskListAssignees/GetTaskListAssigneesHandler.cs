using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Application;

public class GetTaskListAssigneesHandler(TaskPlanDbContext db) : IQueryHandler<GetTaskListAssigneesQuery, List<TaskAssigneeOptionDto>>
{
    public async Task<Result<List<TaskAssigneeOptionDto>>> Handle(GetTaskListAssigneesQuery request, CancellationToken ct)
    {
        var folder = await db.ProjectFolders.FindAsync(request.ListId, ct);
        if (folder == null) return FolderError.NotFound;

        var members = await db.Database.GetDbConnection().QueryAsync<TaskAssigneeOptionDto>(
            GetTaskListAssigneesSQL.GetWorkspaceMembers, 
            new { WorkspaceId = folder.ProjectWorkspaceId });

        return members.ToList();
    }
}


