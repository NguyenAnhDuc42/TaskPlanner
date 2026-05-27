using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Application;

public class GetFolderTasksHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext, CursorHelper cursorHelper) : IQueryHandler<GetFolderTasksQuery, PagedResult<TaskRecord>>
{
    public async Task<Result<PagedResult<TaskRecord>>> Handle(GetFolderTasksQuery request, CancellationToken ct)
    {
        var workspaceId = workspaceContext.workspaceId;
        var connection = db.Database.GetDbConnection();

        var sql = @"
            SELECT id AS Id, name AS Name, created_at AS CreatedAt, status_id AS StatusId, priority AS Priority, due_date AS DueDate, start_date AS StartDate, order_key AS OrderKey, custom_icon as Icon, custom_color as Color, project_folder_id AS ProjectFolderId, project_space_id AS ProjectSpaceId, project_workspace_id AS WorkspaceId
            FROM project_tasks
            WHERE project_workspace_id = @WorkspaceId 
              AND deleted_at IS NULL 
              AND is_archived = false
              AND project_folder_id = @FolderId";

        var parameters = new DynamicParameters();
        parameters.Add("WorkspaceId", workspaceId);
        parameters.Add("FolderId", request.FolderId);
        parameters.Add("Limit", request.Limit);

        var cursorData = request.Cursor != null ? cursorHelper.DecodeCursor(request.Cursor) : null;
        var cursorOrderKey = cursorData?.Values.GetValueOrDefault("OrderKey")?.ToString();
        var cursorTaskId = cursorData?.Values.GetValueOrDefault("Id")?.ToString();

        if (cursorOrderKey != null)
        {
            sql += " AND (order_key > @CursorOrderKey OR (order_key = @CursorOrderKey AND id > @CursorTaskId::uuid))";
            parameters.Add("CursorOrderKey", cursorOrderKey);
            parameters.Add("CursorTaskId", cursorTaskId != null ? (Guid?)Guid.Parse(cursorTaskId) : null);
        }

        if (request.Filter != null)
        {
            if (!string.IsNullOrWhiteSpace(request.Filter.Search))
            {
                sql += " AND name ILIKE @Search";
                parameters.Add("Search", $"%{request.Filter.Search}%");
            }
            if (request.Filter.StatusIds?.Any() == true)
            {
                sql += " AND status_id = ANY(@StatusIds)";
                parameters.Add("StatusIds", request.Filter.StatusIds);
            }
            if (request.Filter.Priorities?.Any() == true)
            {
                sql += " AND priority = ANY(@Priorities)";
                // Pass list of int/enums to dapper
                parameters.Add("Priorities", request.Filter.Priorities.Select(p => (int)p).ToList());
            }
            if (request.Filter.StartDate.HasValue)
            {
                sql += " AND start_date >= @StartDate";
                parameters.Add("StartDate", request.Filter.StartDate.Value);
            }
            if (request.Filter.DueDate.HasValue)
            {
                sql += " AND due_date <= @DueDate";
                parameters.Add("DueDate", request.Filter.DueDate.Value);
            }
        }

        // We will need to handle AssigneeIds via a join with task_assignees later if they want it.
        // For now we do not join on assignees table in the basic query unless requested.

        parameters.Add("LimitPlusOne", request.Limit + 1);
        sql += " ORDER BY order_key, id LIMIT @LimitPlusOne;";

        var allTasks = (await connection.QueryAsync<TaskRecord>(sql, parameters)).AsList();

        bool hasMore = allTasks.Count > request.Limit;
        var tasksToReturn = hasMore ? allTasks.Take(request.Limit).ToList() : allTasks;
        
        string? nextCursor = null;
        var last = tasksToReturn.LastOrDefault();
        
        if (hasMore && last != null)
        {
            var data = new CursorData(new Dictionary<string, object>
            {
                { "OrderKey", last.OrderKey ?? string.Empty },
                { "Id", last.Id.ToString() }
            });
            nextCursor = cursorHelper.EncodeCursor(data);
        }

        return Result<PagedResult<TaskRecord>>.Success(new PagedResult<TaskRecord>(tasksToReturn, nextCursor, hasMore));
    }
}
