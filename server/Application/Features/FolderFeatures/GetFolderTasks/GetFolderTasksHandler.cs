using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Application;

public class GetFolderTasksHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    CursorHelper cursorHelper)
    : IQueryHandler<GetFolderTasksQuery, PagedResult<TaskRecord>>
{
    private const string BaseSelect = @"
        SELECT  id                      AS Id,
                name                    AS Name,
                created_at              AS CreatedAt,
                status_id               AS StatusId,
                priority                AS Priority,
                due_date                AS DueDate,
                start_date              AS StartDate,
                order_key               AS OrderKey,
                custom_icon             AS Icon,
                custom_color            AS Color,
                project_folder_id       AS FolderId,
                project_space_id        AS SpaceId,
                project_workspace_id    AS WorkspaceId
        FROM project_tasks
        /**where**/
        ORDER BY order_key, id
        LIMIT @LimitPlusOne";

    public async Task<Result<PagedResult<TaskRecord>>> Handle(
        GetFolderTasksQuery request,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();

        var builder = new SqlBuilder();
        var template = builder.AddTemplate(BaseSelect);
        var parameters = new DynamicParameters();

        ApplyBaseFilters(builder, parameters, workspaceContext, request.FolderId);

        var cursorData = request.Cursor is not null
            ? cursorHelper.DecodeCursor(request.Cursor)
            : null;

        if (cursorData is not null)
            ApplyCursor(builder, parameters, cursorData);

        if (request.Filter is not null)
            ApplyFilters(builder, parameters, request.Filter);

        parameters.Add("LimitPlusOne", request.Limit + 1);

        var raw = (await connection.QueryAsync<TaskRecord>(template.RawSql, parameters)).AsList();

        return Result<PagedResult<TaskRecord>>.Success(
            BuildPagedResult(raw, request.Limit));
    }

    private static void ApplyBaseFilters(
        SqlBuilder builder,
        DynamicParameters parameters,
        WorkspaceContext workspaceContext,
        Guid folderId)
    {
        builder.Where("project_workspace_id = @WorkspaceId");
        builder.Where("deleted_at IS NULL");
        builder.Where("is_archived = false");
        builder.Where("project_folder_id = @FolderId");
        builder.Where("parent_task_id IS NULL");

        var role = (int)workspaceContext.CurrentMember.Role;
        if (role < (int)Role.Admin)
        {
            builder.Where(@"
                EXISTS (
                    SELECT 1 FROM project_spaces ps
                    WHERE ps.id = project_tasks.project_space_id
                    AND (
                        ps.is_private = false
                        OR EXISTS (
                            SELECT 1 FROM entity_access ea
                            WHERE ea.project_space_id = ps.id
                            AND ea.workspace_member_id = @WorkspaceMemberId
                            AND ea.access_level = ANY(@ValidAccessLevels)
                            AND ea.deleted_at IS NULL
                        )
                    )
                )
            ");
            parameters.Add("WorkspaceMemberId", workspaceContext.CurrentMember.Id);
            parameters.Add("ValidAccessLevels", new[] { "Viewer", "Editor", "Manager" });
        }

        parameters.Add("WorkspaceId", workspaceContext.WorkspaceId);
        parameters.Add("FolderId", folderId);
    }

    private static void ApplyCursor(
        SqlBuilder builder,
        DynamicParameters parameters,
        CursorData cursorData)
    {
        var orderKey = cursorData.Values.GetValueOrDefault("OrderKey")?.ToString();
        var rawId = cursorData.Values.GetValueOrDefault("Id")?.ToString();

        if (orderKey is null) return;

        builder.Where("(order_key > @CursorOrderKey OR (order_key = @CursorOrderKey AND id > @CursorTaskId::uuid))");
        parameters.Add("CursorOrderKey", orderKey);
        parameters.Add("CursorTaskId", rawId is not null ? Guid.Parse(rawId) : (Guid?)null);
    }

    private static void ApplyFilters(
        SqlBuilder builder,
        DynamicParameters parameters,
        TaskFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            builder.Where("name ILIKE @Search");
            parameters.Add("Search", $"%{filter.Search}%");
        }

        if (filter.StatusIds is { Count: > 0 })
        {
            builder.Where("status_id = ANY(@StatusIds)");
            parameters.Add("StatusIds", filter.StatusIds);
        }

        if (filter.Priorities is { Count: > 0 })
        {
            builder.Where("priority = ANY(@Priorities)");
            parameters.Add("Priorities", filter.Priorities.Select(p => p.ToString()).ToArray());
        }

        if (filter.StartDate.HasValue)
        {
            builder.Where("start_date >= @StartDate");
            parameters.Add("StartDate", filter.StartDate.Value);
        }

        if (filter.DueDate.HasValue)
        {
            builder.Where("due_date <= @DueDate");
            parameters.Add("DueDate", filter.DueDate.Value);
        }

        if (filter.AssigneeIds is { Count: > 0 })
        {
            builder.Where(@"
                EXISTS (
                    SELECT 1 FROM task_assignments ta 
                    WHERE ta.project_task_id = project_tasks.id 
                    AND ta.workspace_member_id = ANY(@AssigneeIds)
                    AND ta.deleted_at IS NULL
                )");
            parameters.Add("AssigneeIds", filter.AssigneeIds);
        }
    }

    private PagedResult<TaskRecord> BuildPagedResult(List<TaskRecord> raw, int limit)
    {
        var hasMore = raw.Count > limit;
        var items = hasMore ? raw.Take(limit).ToList() : raw;
        var nextCursor = BuildNextCursor(items, hasMore);

        return new PagedResult<TaskRecord>(items, nextCursor, hasMore);
    }

    private string? BuildNextCursor(List<TaskRecord> items, bool hasMore)
    {
        if (!hasMore) return null;

        var last = items.LastOrDefault();
        if (last is null) return null;

        var data = new CursorData(new Dictionary<string, object>
        {
            { "OrderKey", last.OrderKey ?? string.Empty },
            { "Id", last.Id.ToString() }
        });

        return cursorHelper.EncodeCursor(data);
    }
}