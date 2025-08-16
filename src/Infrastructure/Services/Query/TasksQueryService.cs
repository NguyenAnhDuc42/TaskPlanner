using System;
using System.Data;
using System.Text;
using Dapper;
using src.Contract;
using src.Domain.DTO;
using src.Helper;
using src.Helper.Filters;
using src.Helper.Results;

namespace src.Infrastructure.Services.Query;

public class TasksQueryService
{
    private readonly IDbConnection _dbConnection;
    public TasksQueryService(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }
    public async Task<PagedResult<TasksSummary>> GetTasksAsync(TaskQuery query, Guid currentUserId, CancellationToken cancellationToken = default)
    {

        // Build the main query
        var (sql, parameters) = BuildTaskQuery(query, currentUserId);
         Console.WriteLine("--- EXECUTING SQL ---");
        Console.WriteLine(sql);
        
        // Execute and get tasks
        var tasks = await _dbConnection.QueryAsync<TaskDto>(sql, parameters);
        var taskList = tasks.ToList();
        
        // Check for next page
        var hasNextPage = taskList.Count > query.PageSize;
        var dataToReturn = hasNextPage ? taskList.Take(query.PageSize).ToList() : taskList;
        
        // Get assignees if needed (batch query to prevent N+1)
        var assigneesDict = new Dictionary<Guid, List<UserSummary>>();
        if (query.IncludeAssignees && dataToReturn.Any())
        {
            assigneesDict = await GetAssigneesForTasks( dataToReturn.Select(t => t.Id).ToArray(), _dbConnection);
        }
        
        // Map to response DTOs
        var taskSummaries = dataToReturn.Select(task => new TaskSummary(
            task.Id,
            task.Name,
            task.DueDate,
            task.Priority,
            assigneesDict.GetValueOrDefault(task.Id, new List<UserSummary>())
        )).ToList();
        
        // Generate next cursor
        var nextCursor = hasNextPage && dataToReturn.Any() 
            ? GenerateNextCursor(dataToReturn.Last(), query.SortBy) 
            : null;
        
        return new PagedResult<TasksSummary>(
            new TasksSummary(taskSummaries),
            nextCursor,
            hasNextPage
        );
    }


    private (string sql, DynamicParameters parameters) BuildTaskQuery(TaskQuery query, Guid currentUserId)
    {
        var parameters = new DynamicParameters();
        var whereConditions = new List<string>();
        var joins = new List<string>();

        var sql = new StringBuilder(@"
            SELECT 
                t.""Id"",
                t.""Name"",
                t.""DueDate"",
                t.""Priority"",
                t.""CreatedAt"",
                t.""UpdatedAt""
            FROM ""Tasks"" t ");

        // Step 1: Build all joins first
        BuildJoins(query, joins);

        // Step 2: Build all WHERE conditions (filters and cursor)
        BuildWhereConditions(query, whereConditions, parameters, currentUserId);
        ApplyCursorPagination(query, whereConditions, parameters);

        // Step 3: Append joins to SQL
        foreach (var join in joins)
        {
            sql.AppendLine(join);
        }

        // Step 4: Append a single, consolidated WHERE clause if any conditions exist
        if (whereConditions.Any())
        {
            sql.AppendLine($"WHERE {string.Join(" AND ", whereConditions)}");
        }

        // Step 5: Add ORDER BY
        BuildOrderBy(sql, query);

        // Limit results (PageSize + 1 to check for next page)
        sql.AppendLine($"LIMIT {query.PageSize + 1}");

        return (sql.ToString(), parameters);
    }
    private static void BuildJoins(TaskQuery query, List<string> joins)
    {
        // Only add joins when actually filtering or including assignee data
        if (query.AssigneeId.HasValue ||
            query.AssignedToMe == true ||
            query.IncludeAssignees)
        {
            joins.Add(@"LEFT JOIN ""UserTasks"" ut ON t.""Id"" = ut.""TaskId"" ");
            joins.Add(@"LEFT JOIN ""Users"" u ON ut.""UserId"" = u.""Id"" ");
        }
    }
    private void BuildWhereConditions(TaskQuery query, List<string> whereConditions, DynamicParameters parameters, Guid currentUserId)
    {
        // Hierarchy filters - use these for index optimization
        if (query.WorkspaceId.HasValue)
        {
            whereConditions.Add(@"t.""WorkspaceId"" = @WorkspaceId");
            parameters.Add("WorkspaceId", query.WorkspaceId.Value);
        }

        if (query.SpaceId.HasValue)
        {
            whereConditions.Add(@"t.""SpaceId"" = @SpaceId");
            parameters.Add("SpaceId", query.SpaceId.Value);
        }

        if (query.FolderId.HasValue)
        {
            whereConditions.Add(@"t.""FolderId"" = @FolderId");
            parameters.Add("FolderId", query.FolderId.Value);
        }

        if (query.ListId.HasValue)
        {
            whereConditions.Add(@"t.""ListId"" = @ListId");
            parameters.Add("ListId", query.ListId.Value);
        }

        if (query.StatusId.HasValue)
        {
            whereConditions.Add(@"t.""StatusId"" = @StatusId");
            parameters.Add("StatusId", query.StatusId.Value);
        }

        // User filters
        if (query.CreatedByMe == true)
        {
            whereConditions.Add(@"t.""CreatorId"" = @CurrentUserId");
            parameters.Add("CurrentUserId", currentUserId);
        }
        else if (query.CreatorId.HasValue)
        {
            whereConditions.Add(@"t.""CreatorId"" = @CreatorId");
            parameters.Add("CreatorId", query.CreatorId.Value);
        }

        if (query.AssignedToMe == true)
        {
            whereConditions.Add(@"ut.""UserId"" = @AssignedUserId");
            parameters.Add("AssignedUserId", currentUserId);
        }
        else if (query.AssigneeId.HasValue)
        {
            whereConditions.Add(@"ut.""UserId"" = @AssigneeId");
            parameters.Add("AssigneeId", query.AssigneeId.Value);
        }

        // Priority filters
        if (query.Priorities?.Any() == true)
        {
            whereConditions.Add(@"t.""Priority"" = ANY(@Priorities)");
            parameters.Add("Priorities", query.Priorities.Cast<int>().ToArray());
        }
        else if (query.Priority.HasValue)
        {
            whereConditions.Add(@"t.""Priority"" = @Priority");
            parameters.Add("Priority", (int)query.Priority.Value);
        }

        // Date filters
        if (query.DueDateBefore.HasValue)
        {
            whereConditions.Add(@"t.""DueDate"" < @DueDateBefore");
            parameters.Add("DueDateBefore", query.DueDateBefore.Value);
        }

        if (query.DueDateAfter.HasValue)
        {
            whereConditions.Add(@"t.""DueDate"" > @DueDateAfter");
            parameters.Add("DueDateAfter", query.DueDateAfter.Value);
        }

        if (query.StartDateBefore.HasValue)
        {
            whereConditions.Add(@"t.""StartDate"" < @StartDateBefore");
            parameters.Add("StartDateBefore", query.StartDateBefore.Value);
        }

        if (query.StartDateAfter.HasValue)
        {
            whereConditions.Add(@"t.""StartDate"" > @StartDateAfter");
            parameters.Add("StartDateAfter", query.StartDateAfter.Value);
        }

        if (query.HasDueDate.HasValue)
        {
            whereConditions.Add(query.HasDueDate.Value ? @"t.""DueDate"" IS NOT NULL" : @"t.""DueDate"" IS NULL");
        }

        if (query.IsOverdue == true)
        {
            whereConditions.Add(@"t.""DueDate"" < @Now AND t.""DueDate"" IS NOT NULL");
            parameters.Add("Now", DateTime.UtcNow);
        }

        // Boolean filters
        if (query.IsPrivate.HasValue)
        {
            whereConditions.Add(@"t.""IsPrivate"" = @IsPrivate");
            parameters.Add("IsPrivate", query.IsPrivate.Value);
        }

        // Archive filter (default to non-archived)
        whereConditions.Add(@"t.""IsArchived"" = @IsArchived");
        parameters.Add("IsArchived", query.IsArchived ?? false);

        // Search functionality
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            whereConditions.Add(@"(t.""Name"" ILIKE @SearchTerm OR t.""Description"" ILIKE @SearchTerm)");
            parameters.Add("SearchTerm", $@"%{query.SearchTerm.Trim()}%");
        }

        // Time estimate range
        if (query.TimeEstimateMin.HasValue)
        {
            whereConditions.Add(@"t.""TimeEstimate"" >= @TimeEstimateMin");
            parameters.Add("TimeEstimateMin", query.TimeEstimateMin.Value);
        }

        if (query.TimeEstimateMax.HasValue)
        {
            whereConditions.Add("t.TimeEstimate <= @TimeEstimateMax");
            parameters.Add("TimeEstimateMax", query.TimeEstimateMax.Value);
        }
    }
    private static void BuildOrderBy(StringBuilder sql, TaskQuery query)
    {
        var direction = query.Direction == SortDirection.Desc ? "DESC" : "ASC";

        var orderClause = query.SortBy switch
        {
            TaskSortBy.CreatedAt => $@"t.""CreatedAt"" {direction}",
            TaskSortBy.UpdatedAt => $@"t.""UpdatedAt"" {direction}",
            TaskSortBy.DueDate => $@"t.""DueDate"" {direction} NULLS LAST",
            TaskSortBy.Priority => $@"t.""Priority"" {direction}",
            TaskSortBy.Name => $@"t.""Name"" {direction}",
            _ => $@"t.""CreatedAt"" {direction}"
        };

        // Always include Id for stable pagination
        sql.AppendLine($@"ORDER BY {orderClause}, t.""Id"" {direction}");
    }
    private async Task<Dictionary<Guid, List<UserSummary>>> GetAssigneesForTasks(Guid[] taskIds, IDbConnection connection)
    {
        const string sql = @"
            SELECT DISTINCT
                ut.""TaskId"", 
                u.""Id"", 
                u.""Name"", 
                u.""Email"",
                uw.""Role""
            FROM ""UserTasks"" ut
            INNER JOIN ""Users"" u ON ut.""UserId"" = u.""Id""
            INNER JOIN ""Tasks"" t ON ut.""TaskId"" = t.""Id""
            INNER JOIN ""UserWorkspaces"" uw ON u.""Id"" = uw.""UserId"" AND t.""WorkspaceId"" = uw.""WorkspaceId""
            WHERE ut.""TaskId"" = ANY(@TaskIds)
            ORDER BY ut.""TaskId"", u.""Name""";

        var assigneeData = await connection.QueryAsync<AssigneeDto>(sql, new { TaskIds = taskIds });

        return assigneeData
            .GroupBy(a => a.TaskId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(a => new UserSummary(a.Id, a.Name, a.Email, a.Role)).ToList()
            );
    }
    private void ApplyCursorPagination(TaskQuery query, List<string> whereConditions, DynamicParameters parameters)
    {
        if (string.IsNullOrEmpty(query.Cursor)) return;

        var cursor = CursorHelper.DecodeCursor(query.Cursor);
        if (cursor == null) return;

        var op = query.Direction == SortDirection.Desc ? "<" : ">";

        switch (query.SortBy)
        {
            case TaskSortBy.CreatedAt:
                whereConditions.Add($@"(t.""CreatedAt"" {op} @CursorTimestamp OR (t.""CreatedAt"" = @CursorTimestamp AND t.""Id"" {op} @CursorId))");
                parameters.Add("CursorTimestamp", cursor.Timestamp);
                parameters.Add("CursorId", cursor.Id);
                break;

            case TaskSortBy.UpdatedAt:
                whereConditions.Add($@"(t.""UpdatedAt"" {op} @CursorTimestamp OR (t.""UpdatedAt"" = @CursorTimestamp AND t.""Id"" {op} @CursorId))");
                parameters.Add("CursorTimestamp", cursor.Timestamp);
                parameters.Add("CursorId", cursor.Id);
                break;

            case TaskSortBy.DueDate:
                var nullValue = query.Direction == SortDirection.Desc ? DateTime.MinValue : DateTime.MaxValue;
                whereConditions.Add($@"(COALESCE(t.""DueDate"", @NullValue) {op} @CursorTimestamp OR (COALESCE(t.""DueDate"", @NullValue) = @CursorTimestamp AND t.""Id"" {op} @CursorId))");
                parameters.Add("CursorTimestamp", cursor.Timestamp ?? nullValue);
                parameters.Add("NullValue", nullValue);
                parameters.Add("CursorId", cursor.Id);
                break;

            case TaskSortBy.Name:
                whereConditions.Add($@"(t.""Name"" {op} @CursorString OR (t.""Name"" = @CursorString AND t.""Id"" {op} @CursorId))");
                parameters.Add("CursorString", cursor.StringValue);
                parameters.Add("CursorId", cursor.Id);
                break;

            case TaskSortBy.Priority:
                whereConditions.Add($@"(t.""Priority"" {op} @CursorInt OR (t.""Priority"" = @CursorInt AND t.""Id"" {op} @CursorId))");
                parameters.Add("CursorInt", cursor.IntValue);
                parameters.Add("CursorId", cursor.Id);
                break;
        }
    }
    
    private static string GenerateNextCursor(TaskDto lastTask, TaskSortBy sortBy)
    {
        var cursorData = sortBy switch
        {
            TaskSortBy.CreatedAt => new CursorData { Id = lastTask.Id, Timestamp = lastTask.CreatedAt },
            TaskSortBy.UpdatedAt => new CursorData { Id = lastTask.Id, Timestamp = lastTask.UpdatedAt },
            TaskSortBy.DueDate => new CursorData { Id = lastTask.Id, Timestamp = lastTask.DueDate },
            TaskSortBy.Name => new CursorData { Id = lastTask.Id, StringValue = lastTask.Name },
            TaskSortBy.Priority => new CursorData { Id = lastTask.Id, IntValue = (int)lastTask.Priority },
            _ => new CursorData { Id = lastTask.Id, Timestamp = lastTask.CreatedAt }
        };
        
        return CursorHelper.EncodeCursor(cursorData);
    }

    private static int GetCursorConditionCount(TaskQuery query)
    {
        return string.IsNullOrEmpty(query.Cursor) ? 0 : 1;
    }
}

