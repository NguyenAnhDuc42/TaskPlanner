using Application.Features.TaskFeatures;
using Application.Helpers;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Dapper;
using Application.Interfaces.Data;

namespace Application.Features.ViewFeatures;

public partial class GetViewDataHandler
{
    private async Task<TaskViewData> FetchTaskBoardData(IDataBase db, ViewDefinition view, Workflow workflow, CancellationToken ct)
    {
        var connection = db.Connection;

        // Using QueryMultipleAsync to get all 3 types in a single database roundtrip
        const string sql = @"
            -- 1. Fetch Statuses
            SELECT id AS StatusId, name, color, category
            FROM statuses
            WHERE workflow_id = @WorkflowId
            ORDER BY CASE category
                WHEN 'NotStarted' THEN 0
                WHEN 'Active' THEN 1
                WHEN 'Done' THEN 2
                WHEN 'Closed' THEN 3
                ELSE 4
            END;

            -- 2. Fetch Folders (Only if at Space level)
            SELECT id, name, created_at AS CreatedAt, status_id AS StatusId, start_date AS StartDate, due_date AS DueDate
            FROM project_folders
            WHERE project_space_id = @SpaceId 
              AND @FolderId IS NULL 
              AND deleted_at IS NULL 
              AND is_archived = false;

            -- 3. Fetch Tasks
            SELECT id, name, created_at AS CreatedAt, status_id AS StatusId, priority, due_date AS DueDate, start_date AS StartDate
            FROM project_tasks
            WHERE project_workspace_id = @WorkspaceId 
              AND deleted_at IS NULL 
              AND is_archived = false
              AND (
                  (@FolderId IS NOT NULL AND project_folder_id = @FolderId) OR
                  (@FolderId IS NULL AND project_space_id = @SpaceId AND project_folder_id IS NULL)
              );";

        var parameters = new {
            WorkflowId = workflow.Id,
            WorkspaceId = view.ProjectWorkspaceId,
            SpaceId = view.ProjectSpaceId,
            FolderId = view.ProjectFolderId
        };

        using var multi = await connection.QueryMultipleAsync(sql, parameters);
        
        var statuses = (await multi.ReadAsync<TaskItemStatusDto>()).ToList();
        var folders = (await multi.ReadAsync<FolderItemDto>()).ToList();
        var tasks = (await multi.ReadAsync<TaskItemDto>()).ToList();

        return new TaskViewData(folders, tasks, statuses);
    }
}

public record TaskViewData(
    List<FolderItemDto> Folders,
    List<TaskItemDto> Tasks,
    List<TaskItemStatusDto> Statuses
);

// Using records with properties (init-only) for better Dapper compatibility 
// while keeping the record benefits.
public record FolderItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? StatusId { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public string? OrderKey { get; init; }
}

public record TaskItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? StatusId { get; init; }
    public Priority? Priority { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public string? OrderKey { get; init; }
}

public record TaskItemStatusDto
{
    public Guid StatusId { get; init; }
    public string Name { get; init; } = null!;
    public string Color { get; init; } = null!;
    public StatusCategory Category { get; init; }
}
