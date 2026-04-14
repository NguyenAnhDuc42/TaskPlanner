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

        // PERFORMANCE OPTIMIZATION: Build a lookup for folders (O(N) instead of O(N^2))
        var folderLookup = rawItems
            .Where(i => i.ItemType == "ProjectFolder")
            .ToLookup(i => i.ParentId);

        var dto = new WorkspaceHierarchyDto
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Spaces = MapSpaces(rawItems, folderLookup)
        };

        return Result<WorkspaceHierarchyDto>.Success(dto);
    }

    private List<SpaceHierarchyDto> MapSpaces(List<HierarchyRawItem> allItems, ILookup<Guid, HierarchyRawItem> folderLookup)
    {
        return allItems
            .Where(i => i.ItemType == "ProjectSpace")
            .OrderBy(i => i.OrderKey)
            .ThenBy(i => i.Id)
            .Select(s => new SpaceHierarchyDto
            {
                Id = s.Id,
                Name = s.Name,
                Color = s.Color ?? "",
                Icon = s.Icon ?? "",
                IsPrivate = s.IsPrivate,
                OrderKey = s.OrderKey,
                Folders = MapFolders(s.Id, folderLookup),
                Tasks = new List<TaskHierarchyDto>() // populated on expand via GetNodeTasks
            }).ToList();
    }

    private List<FolderHierarchyDto> MapFolders(Guid spaceId, ILookup<Guid, HierarchyRawItem> folderLookup)
    {
        // PERFORMANCE: O(1) lookup vs O(N) scan
        return folderLookup[spaceId]
            .OrderBy(f => f.OrderKey)
            .ThenBy(f => f.Id)
            .Select(f => new FolderHierarchyDto
            {
                Id = f.Id,
                Name = f.Name,
                Color = f.Color ?? "",
                Icon = f.Icon ?? "",
                IsPrivate = f.IsPrivate,
                OrderKey = f.OrderKey,
                Tasks = new List<TaskHierarchyDto>() // populated on expand via GetNodeTasks
            }).ToList();
    }

    private class HierarchyRawItem
    {
        public string ItemType { get; set; } = null!;
        public Guid Id { get; set; }
        public Guid ParentId { get; set; } // Native Guid instead of string
        public string Name { get; set; } = null!;
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public bool IsPrivate { get; set; }
        public string OrderKey { get; set; } = null!;
    }
}
