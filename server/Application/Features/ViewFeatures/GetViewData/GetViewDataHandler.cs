using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features.TaskFeatures;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ViewFeatures;

public class GetViewDataHandler(IDataBase db) 
    : IQueryHandler<GetViewDataQuery, ViewDataResponse>
{
    public async Task<Result<ViewDataResponse>> Handle(GetViewDataQuery request, CancellationToken ct)
    {
        var view = await db.ViewDefinitions.FirstOrDefaultAsync(v => v.Id == request.ViewId, ct);
        if (view == null) return Result<ViewDataResponse>.Failure(Error.NotFound("View.NotFound", "View definition not found."));

        var workflow = await WorkflowHelper.GetActiveWorkflow(db, view.ProjectWorkspaceId, 
            view.LayerType == EntityLayerType.ProjectSpace ? view.LayerId : null, 
            view.LayerType == EntityLayerType.ProjectFolder ? view.LayerId : null, ct);

        object data;
        switch (view.ViewType)
        {
            case ViewType.Tasks when view.LayerType == EntityLayerType.ProjectSpace:
                data = await FetchTaskBoardData(view, workflow, ct);
                break;
            case ViewType.Tasks:
                data = await FetchTaskBoardData(view, workflow, ct);
                break;
            case ViewType.Overview:
                data = await FetchOverviewContextData(view, workflow, ct);
                break;
            default:
                return Result<ViewDataResponse>.Failure(Error.Validation("View.InvalidType", "Unsupported view type."));
        }

        return Result<ViewDataResponse>.Success(new ViewDataResponse(view.Id, view.ViewType, data));
    }

    private async Task<TaskViewData> FetchTaskBoardData(ViewDefinition view, Workflow workflow, CancellationToken ct)
    {
        var folders = new List<FolderItemDto>();
        if (view.LayerType == EntityLayerType.ProjectSpace)
        {
            folders = await db.Folders
                .AsNoTracking()
                .Where(f => f.ProjectSpaceId == view.LayerId && !f.IsArchived)
                .Select(f => new FolderItemDto(f.Id, f.Name, f.CreatedAt, f.WorkflowId, f.StatusId))
                .ToListAsync(ct);
        }

        var tasksQuery = db.Tasks.AsNoTracking().Where(t => t.ProjectWorkspaceId == view.ProjectWorkspaceId && !t.IsArchived);
        if (view.LayerType == EntityLayerType.ProjectSpace)
            tasksQuery = tasksQuery.Where(t => t.ProjectSpaceId == view.LayerId && t.ProjectFolderId == null);
        else if (view.LayerType == EntityLayerType.ProjectFolder)
            tasksQuery = tasksQuery.Where(t => t.ProjectFolderId == view.LayerId);

        tasksQuery = tasksQuery.ApplyFilters(view.FilterConfig);

        var tasks = await tasksQuery
            .Select(t => new TaskItemDto(t.Id, t.Name, t.CreatedAt, t.StatusId, t.Priority, t.DueDate))
            .ToListAsync(ct);

        var groups = new List<ExplorerStatusGroupDto>();

        var noStatusFolders = folders.Where(f => !f.StatusId.HasValue || !workflow.Statuses.Any(s => s.Id == f.StatusId)).ToList();
        var noStatusTasks = tasks.Where(t => t.StatusId == null || !workflow.Statuses.Any(s => s.Id == t.StatusId)).ToList();
        if (noStatusFolders.Any() || noStatusTasks.Any())
        {
            groups.Add(new ExplorerStatusGroupDto(Guid.Empty, "No Status", StatusCategory.NotStarted, "#94a3b8", noStatusFolders, noStatusTasks));
        }

        foreach (var status in workflow.Statuses.OrderBy(s => s.Category).ThenBy(s => s.Name))
        {
            groups.Add(new ExplorerStatusGroupDto(
                status.Id, status.Name, status.Category, status.Color,
                folders.Where(f => f.StatusId == status.Id).ToList(),
                tasks.Where(t => t.StatusId == status.Id).ToList()));
        }

        return new TaskViewData(groups);
    }

    private async Task<OverviewViewData> FetchOverviewContextData(ViewDefinition view, Workflow workflow, CancellationToken ct)
    {
        string name = "Unknown";
        string? description = null;
        Guid? statusId = null;
        Guid creatorId = Guid.Empty;
        DateTimeOffset createdAt = DateTimeOffset.Now;

        if (view.LayerType == EntityLayerType.ProjectSpace)
        {
            var space = await db.Spaces.FirstOrDefaultAsync(s => s.Id == view.LayerId, ct);
            if (space != null)
            {
                name = space.Name;
                description = space.Description;
                statusId = space.StatusId;
                creatorId = space.CreatorId ?? Guid.Empty;
                createdAt = space.CreatedAt;
            }
        }
        else if (view.LayerType == EntityLayerType.ProjectFolder)
        {
            var folder = await db.Folders.FirstOrDefaultAsync(f => f.Id == view.LayerId, ct);
            if (folder != null)
            {
                name = folder.Name;
                description = folder.Description;
                statusId = folder.StatusId;
                creatorId = folder.CreatorId ?? Guid.Empty;
                createdAt = folder.CreatedAt;
            }
        }

        var taskQuery = db.Tasks.Where(t => t.ProjectWorkspaceId == view.ProjectWorkspaceId && !t.IsArchived);
        int folderCount = 0;
        if (view.LayerType == EntityLayerType.ProjectSpace)
        {
            taskQuery = taskQuery.Where(t => t.ProjectSpaceId == view.LayerId);
            folderCount = await db.Folders.CountAsync(f => f.ProjectSpaceId == view.LayerId && !f.IsArchived, ct);
        }
        else if (view.LayerType == EntityLayerType.ProjectFolder)
        {
            taskQuery = taskQuery.Where(t => t.ProjectFolderId == view.LayerId);
        }

        var taskCount = await taskQuery.CountAsync(ct);

        return new OverviewViewData(
            view.LayerId, 
            name, 
            description, 
            statusId, 
            (Guid?)workflow.Id, 
            null, 
            creatorId, 
            createdAt, 
            new OverviewStats(taskCount, folderCount));
    }
}
