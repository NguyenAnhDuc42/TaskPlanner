using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces.Data;
using Dapper;

namespace Application.Features.ViewFeatures;

public partial class GetViewDataHandler
{
    private async Task<OverviewViewData> FetchOverviewContextData(IDataBase db, ViewDefinition view, Workflow parentWorkflow, Workflow activeWorkflow, CancellationToken ct)
    {
        var connection = db.Connection;

        // Using QueryMultipleAsync for a single database roundtrip
        string entitySql = view.ProjectFolderId != null 
            ? "SELECT name, description, custom_color AS Color, custom_icon AS Icon, status_id AS StatusId, start_date AS StartDate, due_date AS DueDate FROM project_folders WHERE id = @Id;"
            : "SELECT name, description, custom_color AS Color, custom_icon AS Icon, status_id AS StatusId, start_date AS StartDate, due_date AS DueDate FROM project_spaces WHERE id = @Id;";

        string sql = $@"
            -- 1. Fetch Entity Details
            {entitySql}

            -- 2. Fetch Task Counts grouped by category
            SELECT s.category, COUNT(*) as Count
            FROM project_tasks t
            LEFT JOIN statuses s ON t.status_id = s.id
            WHERE t.project_workspace_id = @WorkspaceId 
              AND t.deleted_at IS NULL 
              AND t.is_archived = false
              AND (
                  (@FolderId IS NOT NULL AND t.project_folder_id = @FolderId) OR
                  (@FolderId IS NULL AND t.project_space_id = @SpaceId)
              )
            GROUP BY s.category;

            -- 3. Fetch Folder Count (if space)
            SELECT COUNT(*) 
            FROM project_folders 
            WHERE project_space_id = @SpaceId 
              AND @FolderId IS NULL 
              AND deleted_at IS NULL 
              AND is_archived = false;";

        var entityId = view.ProjectFolderId ?? view.ProjectSpaceId ?? Guid.Empty;
        var parameters = new {
            Id = entityId,
            WorkspaceId = view.ProjectWorkspaceId,
            SpaceId = view.ProjectSpaceId,
            FolderId = view.ProjectFolderId
        };

        using var multi = await connection.QueryMultipleAsync(sql, parameters);
        
        var entity = await multi.ReadFirstOrDefaultAsync<dynamic>();
        var countResults = (await multi.ReadAsync<dynamic>()).ToList();
        var folderCount = await multi.ReadFirstAsync<int>();

        // Map Entity Details
        string name = entity?.name ?? "Unknown";
        string? description = entity?.description;
        string? color = entity?.color;
        string? icon = entity?.icon;
        Guid? statusId = entity?.statusid;
        DateTimeOffset? startDate = entity?.startdate;
        DateTimeOffset? dueDate = entity?.duedate;

        // Map Status (Status comes from Parent Workflow)
        OverviewStatusDto? statusDto = null;
        if (statusId != null)
        {
            var status = parentWorkflow.Statuses.FirstOrDefault(s => s.Id == statusId);
            if (status != null)
            {
                statusDto = new OverviewStatusDto(status.Name, status.Category.ToString(), status.Color);
            }
        }

        // Map Task Progress
        int totalTasks = 0;
        int completedTasks = 0;
        foreach (var row in countResults)
        {
            int count = (int)row.count;
            totalTasks += count;
            if (row.category != null)
            {
                string categoryStr = row.category.ToString();
                if (categoryStr == "Done" || categoryStr == "Closed")
                {
                    completedTasks += count;
                }
            }
        }

        return new OverviewViewData(
            entityId, 
            name, 
            color,
            icon,
            description, 
            statusDto, 
            activeWorkflow.Name, 
            new OverviewProgressDto(completedTasks, totalTasks),
            new List<OverviewActivityDto>(),
            new OverviewStats(totalTasks, folderCount),
            startDate,
            dueDate,
            parentWorkflow.Statuses.Select(s => new OverviewStatusItemDto(s.Id, s.Name, s.Category.ToString(), s.Color)).ToList(),
            view.ProjectSpaceId != null ? new OverviewTimeDto("0h", "0h", "0h") : null
        );
    }
}

public record OverviewViewData(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    string? Description,
    OverviewStatusDto? Status,
    string? WorkflowName,
    OverviewProgressDto Progress,
    List<OverviewActivityDto> RecentActivity,
    OverviewStats Stats,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? DueDate = null,
    List<OverviewStatusItemDto> AvailableStatuses = null!,
    OverviewTimeDto? TimeStats = null
);

public record OverviewStatusItemDto(
    Guid Id,
    string Name,
    string Category,
    string Color
);

public record OverviewStatusDto(
    string Name,
    string Category,
    string Color
);

public record OverviewProgressDto(
    int CompletedTasks,
    int TotalTasks
);

public record OverviewActivityDto(
    Guid Id,
    string Content,
    string Type,
    DateTimeOffset Timestamp
);

public record OverviewTimeDto(
    string? TimeEstimate = null,
    string? TimeLogged = null,
    string? RemainingTime = null
);

public record OverviewStats(
    int TotalTasks,
    int TotalFolders
);
