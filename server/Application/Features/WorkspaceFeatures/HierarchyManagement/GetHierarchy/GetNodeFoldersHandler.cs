using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces.Data;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public class GetNodeFoldersHandler(IDataBase db) : IQueryHandler<GetNodeFoldersQuery, List<FolderHierarchyDto>>
{
    public async Task<Result<List<FolderHierarchyDto>>> Handle(GetNodeFoldersQuery request, CancellationToken ct)
    {
        // PERFORMANCE: Using the specialized covering index idx_folders_workspace_space_order
        var rawFolders = (await db.QueryAsync<FolderRaw>(
            GetHierarchySql.GetFoldersBySpaceQuery, 
            new { SpaceId = request.NodeId }, 
            cancellationToken: ct)).ToList();

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
                HasTasks = f.HasTasks != 0,
                Tasks = new()
            });
        }

        return Result<List<FolderHierarchyDto>>.Success(dtos);
    }

    private record FolderRaw(Guid Id, Guid ParentId, string Name, string? Color, string? Icon, bool IsPrivate, string OrderKey, int HasTasks);
}
