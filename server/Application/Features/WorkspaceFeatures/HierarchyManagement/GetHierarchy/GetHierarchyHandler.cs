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
    public GetHierarchyHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<WorkspaceHierarchyDto> Handle(GetHierarchyQuery request, CancellationToken cancellationToken)
    {
        var workspace =  await UnitOfWork.Set<ProjectWorkspace>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.WorkspaceId, cancellationToken);
        if (workspace == null) throw new KeyNotFoundException($"Workspace {request.WorkspaceId} not found");

        var rawItems = (await UnitOfWork.QueryAsync<HierarchyRawItem>(GetHierarchySql.StructureQuery, new 
        { 
            WorkspaceId = request.WorkspaceId, 
            UserId = CurrentUserId 
        }, cancellationToken)).ToList();

        return new WorkspaceHierarchyDto
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Spaces = MapSpaces(rawItems)
        };
    }

    private List<SpaceHierarchyDto> MapSpaces(List<HierarchyRawItem> allItems)
    {
        var spaces = allItems.Where(i => i.ItemType == "Space");
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
