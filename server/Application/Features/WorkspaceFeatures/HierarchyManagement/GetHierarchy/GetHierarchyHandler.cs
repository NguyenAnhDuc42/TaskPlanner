using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public class GetHierarchyHandler(IDataBase db, WorkspaceContext context) : IQueryHandler<GetHierarchyQuery, WorkspaceHierarchyDto>
{
    public async Task<Result<WorkspaceHierarchyDto>> Handle(GetHierarchyQuery request, CancellationToken ct)
    {
        var workspace = await db.Workspaces
            .AsNoTracking()
            .WhereNotDeleted()
            .FirstOrDefaultAsync(x => x.Id == request.WorkspaceId, ct);

        if (workspace == null) 
            return Result<WorkspaceHierarchyDto>.Failure(Error.NotFound("Workspace.NotFound", $"Workspace {request.WorkspaceId} not found"));

        var rawItems = (await db.QueryAsync<HierarchyRawItem>(GetHierarchySql.StructureQuery, new 
        { 
            WorkspaceId = request.WorkspaceId, 
            UserId = context.CurrentMember.UserId 
        }, cancellationToken: ct)).ToList();

        var dto = new WorkspaceHierarchyDto
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Spaces = BuildHierarchy(rawItems)
        };

        return Result<WorkspaceHierarchyDto>.Success(dto);
    }

    private static List<SpaceHierarchyDto> BuildHierarchy(List<HierarchyRawItem> rawItems)
    {
        var spaces = new List<SpaceHierarchyDto>();
        var foldersBySpace = new Dictionary<Guid, List<FolderHierarchyDto>>();

        // PERFORMANCE: Single pass O(N) mapping. 
        // SQL query already orders by OrderKey and Id, so insertion order guarantees perfectly sorted lists without any .Sort() calls.
        foreach (var item in rawItems)
        {
            if (item.ItemType == "ProjectSpace")
            {
                spaces.Add(new SpaceHierarchyDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    Color = item.Color ?? "",
                    Icon = item.Icon ?? "",
                    IsPrivate = item.IsPrivate,
                    OrderKey = item.OrderKey,
                    Folders = new(),
                    Tasks = new()
                });
            }
            else if (item.ItemType == "ProjectFolder")
            {
                if (!foldersBySpace.TryGetValue(item.ParentId, out var folderList))
                {
                    folderList = new List<FolderHierarchyDto>();
                    foldersBySpace[item.ParentId] = folderList;
                }

                folderList.Add(new FolderHierarchyDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    Color = item.Color ?? "",
                    Icon = item.Icon ?? "",
                    IsPrivate = item.IsPrivate,
                    OrderKey = item.OrderKey,
                    Tasks = new()
                });
            }
        }

        // Attach folders to spaces
        foreach (var space in spaces)
        {
            if (foldersBySpace.TryGetValue(space.Id, out var spaceFolders))
                space.Folders.AddRange(spaceFolders);
        }

        return spaces;
    }

    private record HierarchyRawItem
    {
        public string ItemType { get; init; } = null!;
        public Guid Id { get; init; }
        public Guid ParentId { get; init; } // Native Guid instead of string
        public string Name { get; init; } = null!;
        public string? Color { get; init; }
        public string? Icon { get; init; }
        public bool IsPrivate { get; init; }
        public string OrderKey { get; init; } = null!;
    }
}
