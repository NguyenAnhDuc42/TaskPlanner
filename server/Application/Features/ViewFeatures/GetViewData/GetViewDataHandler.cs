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
using Application.Interfaces;

namespace Application.Features.ViewFeatures;

public class GetViewDataHandler(IDataBase db) 
    : IQueryHandler<GetViewDataQuery, ViewDataResponse>
{
    public async Task<Result<ViewDataResponse>> Handle(GetViewDataQuery request, CancellationToken ct)
    {
        var view = await db.ViewDefinitions.FirstOrDefaultAsync(v => v.Id == request.ViewId, ct);
        if (view == null) return Result<ViewDataResponse>.Failure(Error.NotFound("View.NotFound", "View definition not found."));

        var workflow = await WorkflowHelper.GetActiveWorkflow(db, view.ProjectWorkspaceId, 
            view.ProjectSpaceId, 
            view.ProjectFolderId, ct);

        object data;
        switch (view.ViewType)
        {
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
        if (view.ProjectSpaceId != null && view.ProjectFolderId == null)
        {
            var spaceId = view.ProjectSpaceId.Value;
            folders = await db.Folders
                .AsNoTracking()
                .Where(f => f.ProjectSpaceId == spaceId && f.DeletedAt == null && !f.IsArchived)
                .Select(f => new FolderItemDto(f.Id, f.Name, f.CreatedAt, f.WorkflowId, f.StatusId))
                .ToListAsync(ct);
        }

        var tasksQuery = db.Tasks.AsNoTracking().Where(t => t.ProjectWorkspaceId == view.ProjectWorkspaceId && t.DeletedAt == null && !t.IsArchived);
        
        if (view.ProjectFolderId != null)
        {
            tasksQuery = tasksQuery.Where(t => t.ProjectFolderId == view.ProjectFolderId);
        }
        else if (view.ProjectSpaceId != null)
        {
            tasksQuery = tasksQuery.Where(t => t.ProjectSpaceId == view.ProjectSpaceId && t.ProjectFolderId == null);
        }

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

        if (view.ProjectFolderId != null)
        {
            var folder = await db.Folders.FirstOrDefaultAsync(f => f.Id == view.ProjectFolderId, ct);
            if (folder != null)
            {
                name = folder.Name;
                description = folder.Description;
                statusId = folder.StatusId;
                creatorId = folder.CreatorId.GetValueOrDefault();
                createdAt = folder.CreatedAt;
            }
        }
        else if (view.ProjectSpaceId != null)
        {
            var space = await db.Spaces.FirstOrDefaultAsync(s => s.Id == view.ProjectSpaceId, ct);
            if (space != null)
            {
                name = space.Name;
                description = space.Description;
                statusId = space.StatusId;
                creatorId = space.CreatorId.GetValueOrDefault();
                createdAt = space.CreatedAt;
            }
        }

        var taskQuery = db.Tasks.Where(t => t.ProjectWorkspaceId == view.ProjectWorkspaceId && t.DeletedAt == null && !t.IsArchived);
        int folderCount = 0;
        
        if (view.ProjectFolderId != null)
        {
            taskQuery = taskQuery.Where(t => t.ProjectFolderId == view.ProjectFolderId);
        }
        else if (view.ProjectSpaceId != null)
        {
            var spaceId = view.ProjectSpaceId.Value;
            taskQuery = taskQuery.Where(t => t.ProjectSpaceId == spaceId);
            folderCount = await db.Folders.CountAsync(f => f.ProjectSpaceId == spaceId && f.DeletedAt == null && !f.IsArchived, ct);
        }

        var taskCount = await taskQuery.CountAsync(ct);

        return new OverviewViewData(
            view.ProjectFolderId ?? view.ProjectSpaceId ?? Guid.Empty, 
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
