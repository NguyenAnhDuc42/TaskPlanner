using Application.Interfaces.Repositories;
using Dapper;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;


namespace Application.Features.DashboardFeatures.WidgetDataHelper;

/// <summary>
/// Specialized DTO for items within a Task List widget.
/// Moved here for feature-specific cohesion.
/// </summary>
public record class TaskStatusItem
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public Guid StatusId { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public Priority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
}

public static class TaskListWidgetSQL
{
        public static async Task<List<TaskStatusItem>> ExecuteAsync(IUnitOfWork unitOfWork, Guid layerId, EntityLayerType layerType, string configJson,Guid workspaceMemberId, CancellationToken ct)
        {
            var builder = new SqlBuilder();
            var selector = builder.AddTemplate(@"
                SELECT 
                    t.id,
                    t.name as title,
                    t.status_id as statusId,
                    t.due_date as dueDate,
                    t.priority,
                    t.created_at as createdAt
                FROM project_tasks t
                INNER JOIN project_spaces s ON t.project_space_id = s.id
                INNER JOIN project_workspaces w ON s.project_workspace_id = w.id
                LEFT JOIN project_folders f ON t.project_folder_id = f.id
                /**where**/
                ORDER BY t.created_at DESC
                LIMIT @limit");

            switch (layerType)
            {
                case EntityLayerType.ProjectWorkspace:
                    builder.Where("w.id = @layerId");
                    break;
                case EntityLayerType.ProjectSpace:
                    builder.Where("s.id = @layerId");
                    break;
                case EntityLayerType.ProjectFolder:
                    builder.Where("f.id = @layerId");
                    break;
            }

        builder.Where(@"
            -- Soft delete
            t.deleted_at IS NULL
            AND s.deleted_at IS NULL
            AND w.deleted_at IS NULL
            AND (f.id IS NULL OR f.deleted_at IS NULL)
        ");


        var parameters = new DynamicParameters();
            parameters.Add("@layerId", layerId);
            parameters.Add("@limit", 50);
            parameters.Add("@workspaceMemberId", workspaceMemberId);

            parameters.Add("@spaceType", EntityLayerType.ProjectSpace.ToString());
            parameters.Add("@folderType", EntityLayerType.ProjectFolder.ToString());
            parameters.Add("@taskType", EntityLayerType.ProjectTask.ToString());

        var tasks = await unitOfWork.QueryAsync<TaskStatusItem>(selector.RawSql, parameters, ct);
            return tasks.ToList();
        }
}
