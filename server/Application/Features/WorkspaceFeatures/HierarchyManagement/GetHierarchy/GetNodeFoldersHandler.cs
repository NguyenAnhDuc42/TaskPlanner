using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Application;

public class GetNodeFoldersHandler(TaskPlanDbContext db) : IQueryHandler<GetNodeFoldersQuery, List<FolderHierarchyDto>>
{
    public async Task<Result<List<FolderHierarchyDto>>> Handle(GetNodeFoldersQuery request, CancellationToken ct)
    {
        var rawFolders = (await db.Database.GetDbConnection().QueryAsync<FolderRaw>(
            GetHierarchySql.GetFoldersBySpaceQuery, 
            new { SpaceId = request.NodeId })).AsList();

        var dtos = new List<FolderHierarchyDto>(rawFolders.Count);
        foreach (var f in rawFolders)
        {
            dtos.Add(new FolderHierarchyDto
            {
                Id = f.Id,
                Name = f.Name,
                Color = f.Color ?? string.Empty,
                Icon = f.Icon ?? string.Empty,
                IsPrivate = f.IsPrivate,
                OrderKey = f.OrderKey,
                HasTasks = f.HasTasks,
                Tasks = new()
            });
        }

        return Result<List<FolderHierarchyDto>>.Success(dtos);
    }

    private record FolderRaw(Guid Id, Guid ParentId, string Name, string? Color, string? Icon, bool IsPrivate, string OrderKey, bool HasTasks);
}


