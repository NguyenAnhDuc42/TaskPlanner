using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public class GetHierarchyHandler : BaseFeatureHandler, IRequestHandler<GetHierarchyQuery, WorkspaceHierarchyDto>
{

    public GetHierarchyHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<WorkspaceHierarchyDto> Handle(GetHierarchyQuery request, CancellationToken cancellationToken)
    {
        var workspace = await UnitOfWork.Set<ProjectWorkspace>().AsNoTracking().FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, cancellationToken);
        if (workspace == null) throw new KeyNotFoundException($"Workspace {request.WorkspaceId} not found");

        var results = await UnitOfWork.QueryAsync<HierarchyRawItem>(GetHierarchySql.Query, new 
        { 
            WorkspaceId = request.WorkspaceId, 
            UserId = CurrentUserId 
        }, cancellationToken);

        var items = results.ToList();
        var spaces = items.Where(i => i.ItemType == "Space").ToList();
        var folders = items.Where(i => i.ItemType == "Folder").ToList();
        var tasks = items.Where(i => i.ItemType == "Task").ToList();

        var spaceHierarchy = spaces.Select(s => new SpaceHierarchyDto
        {
            Id = s.Id,
            Name = s.Name,
            Color = s.Color ?? "",
            Icon = s.Icon ?? "",
            IsPrivate = s.IsPrivate,
            Folders = folders
                .Where(f => Guid.Parse(f.ParentId) == s.Id)
                .Select(f => new FolderHierarchyDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Color = f.Color ?? "",
                    Icon = f.Icon ?? "",
                    IsPrivate = f.IsPrivate,
                    Tasks = tasks
                        .Where(t => t.ParentId == f.Id.ToString())
                        .Select(MapToTaskDto)
                        .ToList()
                })
                .ToList(),
            Tasks = tasks
                .Where(t => t.ParentId == s.Id.ToString())
                .Select(MapToTaskDto)
                .ToList()
        }).ToList();

        return new WorkspaceHierarchyDto
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Spaces = spaceHierarchy
        };
    }

    private static TaskHierarchyDto MapToTaskDto(HierarchyRawItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        StatusId = item.StatusId,
        Priority = item.Priority,
        Color = "", // Specific task customization can be added later if needed
        Icon = ""
    };

    private class HierarchyRawItem
    {
        public string ItemType { get; set; } = null!;
        public Guid Id { get; set; }
        public string ParentId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public bool IsPrivate { get; set; }
        public long OrderKey { get; set; }
        public Guid? StatusId { get; set; }
        public Priority Priority { get; set; }
        public int SortGroup { get; set; }
    }
}
