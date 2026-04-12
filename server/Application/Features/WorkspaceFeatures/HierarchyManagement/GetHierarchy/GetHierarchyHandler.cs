using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;

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
            Spaces = MapSpaces(rawItems)
        };

        return Result<WorkspaceHierarchyDto>.Success(dto);
    }

    private List<SpaceHierarchyDto> MapSpaces(List<HierarchyRawItem> allItems)
    {
        var spaces = allItems.Where(i => i.ItemType == "Space").OrderBy(i => i.OrderKey).ThenBy(i => i.Id);
        var folders = allItems.Where(i => i.ItemType == "Folder").ToList();

        return spaces.Select(s => new SpaceHierarchyDto
        {
            Id = s.Id,
            Name = s.Name,
            Color = s.Color ?? "",
            Icon = s.Icon ?? "",
            IsPrivate = s.IsPrivate,
            OrderKey = s.OrderKey,
            Folders = MapFolders(s.Id, folders),
            Tasks = new List<TaskHierarchyDto>() // populated on expand via GetNodeTasks
        }).ToList();
    }

    private List<FolderHierarchyDto> MapFolders(Guid spaceId, List<HierarchyRawItem> folders)
    {
        return folders
            .Where(f => Guid.Parse(f.ParentId) == spaceId)
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
        public string ParentId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public bool IsPrivate { get; set; }
        public string OrderKey { get; set; } = null!;
    }
}
