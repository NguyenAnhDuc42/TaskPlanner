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
        var workspace = await (from w in UnitOfWork.Set<ProjectWorkspace>().AsNoTracking()
                              where w.Id == request.WorkspaceId
                              select w).FirstOrDefaultAsync(cancellationToken);

        if (workspace == null) throw new KeyNotFoundException($"Workspace {request.WorkspaceId} not found");

        var rawItems = (await UnitOfWork.QueryAsync<HierarchyRawItem>(GetHierarchySql.Query, new 
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
        var spaces = from i in allItems where i.ItemType == "Space" select i;
        var folders = (from i in allItems where i.ItemType == "Folder" select i).ToList();
        var tasks = (from i in allItems where i.ItemType == "Task" select i).ToList();

        return (from s in spaces
                select new SpaceHierarchyDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Color = s.Color ?? "",
                    Icon = s.Icon ?? "",
                    IsPrivate = s.IsPrivate,
                    Folders = MapFolders(s.Id, folders, tasks),
                    Tasks = MapTasks(s.Id.ToString(), tasks)
                }).ToList();
    }

    private List<FolderHierarchyDto> MapFolders(Guid spaceId, List<HierarchyRawItem> folders, List<HierarchyRawItem> tasks)
    {
        return (from f in folders
                where Guid.Parse(f.ParentId) == spaceId
                select new FolderHierarchyDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Color = f.Color ?? "",
                    Icon = f.Icon ?? "",
                    IsPrivate = f.IsPrivate,
                    Tasks = MapTasks(f.Id.ToString(), tasks)
                }).ToList();
    }

    private List<TaskHierarchyDto> MapTasks(string parentId, List<HierarchyRawItem> tasks)
    {
        return (from t in tasks
                where t.ParentId == parentId
                select new TaskHierarchyDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    StatusId = t.StatusId,
                    Priority = t.Priority,
                    Color = "",
                    Icon = ""
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
        public long OrderKey { get; set; }
        public Guid? StatusId { get; set; }
        public Priority Priority { get; set; }
        public int SortGroup { get; set; }
    }
}
