using Microsoft.EntityFrameworkCore;


namespace Application;

public class GetHierarchyHandler(TaskPlanDbContext db) : IQueryHandler<GetHierarchyQuery, WorkspaceHierarchyRecord>
{
    public async Task<Result<WorkspaceHierarchyRecord>> Handle(GetHierarchyQuery request, CancellationToken ct)
    {
        var rows = await db.Database.SqlQueryRaw<SpaceRow>(
            GetHierarchySql.GetSpacesAndWorkspaceQuery,
            new Npgsql.NpgsqlParameter("WorkspaceId", request.WorkspaceId)
        ).ToListAsync(ct);

        if (rows.Count == 0)
            return Result<WorkspaceHierarchyRecord>.Failure(
                Error.NotFound("Workspace.NotFound",
                    $"Workspace {request.WorkspaceId} not found"));

        return Result<WorkspaceHierarchyRecord>.Success(BuildHierarchy(rows));
    }

    private static WorkspaceHierarchyRecord BuildHierarchy(List<SpaceRow> rows)
    {
        var spaces = new List<SpaceRecord>();

        foreach (var s in rows)
        {
            if (s.Id == null) continue;

            spaces.Add(new SpaceRecord
            {
                Id        = s.Id.Value,
                Name      = s.Name ?? string.Empty,
                Color     = s.Color,
                Icon      = s.Icon,
                IsPrivate = s.IsPrivate ?? false,
                OrderKey  = s.OrderKey,
                HasFolders = s.HasFolders ?? false,
                HasTasks   = s.HasTasks ?? false
            });
        }

        return new WorkspaceHierarchyRecord
        {
            Name   = rows[0].WorkspaceName,
            Spaces = spaces
        };
    }

    private record SpaceRow(
        string WorkspaceName,
        Guid? Id, string? Name, string? Color, string? Icon,
        bool? IsPrivate, string? OrderKey,
        bool? HasFolders, bool? HasTasks);
}


