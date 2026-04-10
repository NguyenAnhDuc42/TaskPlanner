using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public class GetHierarchyHandler : IQueryHandler<GetHierarchyQuery, WorkspaceHierarchyDto>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;

    public GetHierarchyHandler(IDataBase db, ICurrentUserService currentUserService) {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<WorkspaceHierarchyDto>> Handle(GetHierarchyQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) return Result.Failure<WorkspaceHierarchyDto>(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        var workspace = await _db.Workspaces
            .AsNoTracking()
            .WhereNotDeleted()
            .FirstOrDefaultAsync(x => x.Id == request.WorkspaceId, cancellationToken);
        if (workspace == null) return Result.Failure<WorkspaceHierarchyDto>(Error.NotFound("Workspace.NotFound", $"Workspace {request.WorkspaceId} not found"));

        var rawItems = (await _db.QueryAsync<HierarchyRawItem>(GetHierarchySql.StructureQuery, new 
        { 
            WorkspaceId = request.WorkspaceId, 
            UserId = currentUserId 
        }, cancellationToken: cancellationToken)).ToList();

        var dto = new WorkspaceHierarchyDto
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Spaces = MapSpaces(rawItems)
        };

        return Result.Success(dto);
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
