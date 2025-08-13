using System;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using src.Contract;
using src.Domain.Enums;
using src.Helper.Filters;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IServices;
using src.Helper; // your CursorHelper namespace (assumed)

namespace src.Feature.TaskManager.GetTasks
{
    public class GetTasksHandler : IRequestHandler<GetTasksRequest, PagedResult<TasksSummary>>
    {
        private readonly IDbConnection _dbConnection;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetTasksHandler> _logger;

        public GetTasksHandler(IDbConnection dbConnection, ICurrentUserService currentUserService, ILogger<GetTasksHandler> logger)
        {
            _dbConnection = dbConnection;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<PagedResult<TasksSummary>> Handle(GetTasksRequest request, CancellationToken cancellationToken)
        {
            var resolvedQuery = ResolveUserContextFilters(request.query);

            var (sql, parameters) = BuildQuery(resolvedQuery);

            // DEBUG: log the SQL and parameter names/values so we can inspect what's sent to Postgres
            // This will reveal if the table name casing/quoting is wrong.
            try
            {
                _logger.LogDebug("GetTasks SQL:\n{Sql}\nParameters: {Params}",
                    sql,
                    parameters.ParameterNames.ToDictionary(n => n, n => parameters.Get<dynamic>(n)));
            }
            catch
            {
                // best-effort logging; don't break flow if reflection fails
                _logger.LogDebug("GetTasks SQL (params unavailable):\n{Sql}", sql);
            }

            var stopwatch = Stopwatch.StartNew();

            var taskDict = new Dictionary<Guid, TaskSummaryBuilder>();

            // Use splitOn "Id" because we select u.Id in the columns
            await _dbConnection.QueryAsync<TaskSummaryBuilder, UserSummary?, TaskSummaryBuilder>(
                sql,
                (task, assignee) =>
                {
                    if (!taskDict.TryGetValue(task.Id, out var existingTask))
                    {
                        taskDict[task.Id] = task;
                        existingTask = task;
                    }
                    
                    if (assignee != null && !existingTask.AssigneeIds.Contains(assignee.Id))
                    {
                        
                        existingTask.Assignees.Add(assignee);
                        existingTask.AssigneeIds.Add(assignee.Id);
                    }

                    return existingTask;
                },
                parameters,
                splitOn: resolvedQuery.IncludeAssignees ? "Id" : null);

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning("Slow task query: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }

            var tasks = taskDict.Values.ToList();
            var hasNextPage = tasks.Count > resolvedQuery.PageSize;

            var result = tasks
                .Take(resolvedQuery.PageSize)
                .Select(t => new TaskSummary(t.Id, t.Name, t.DueDate, t.Priority, t.Assignees))
                .ToList();

            var nextCursor = hasNextPage
                ? GenerateNextCursor(tasks[resolvedQuery.PageSize - 1], resolvedQuery.SortBy)
                : null;

            var pagedResult = new PagedResult<TasksSummary>(
                new TasksSummary(result),
                nextCursor,
                hasNextPage);

            return pagedResult;
        }

        #region Query Builders - Split for readability

        private (string Sql, DynamicParameters Parameters) BuildQuery(TaskQuery query)
        {
            var sb = new StringBuilder();
            var parameters = new DynamicParameters();

            // NOTE: call the actual method implemented below
            AppendSelectAndJoins(sb, query);
            AppendWhereBase(sb);
            AppendHierarchyFilters(sb, parameters, query);
            AppendUserFilters(sb, parameters, query);
            AppendPriorityFilters(sb, parameters, query);
            AppendDateFilters(sb, parameters, query);
            AppendArchiveAndSearch(sb, parameters, query);
            AppendCursorPagination(sb, parameters, query); // composite cursor AND id tie-breaker
            AppendOrderAndLimit(sb, parameters, query);

            return (sb.ToString(), parameters);
        }

        private void AppendSelectAndJoins(StringBuilder sb, TaskQuery query)
        {
            // Build column list deterministically (no leading commas)
            // Keep column names matching the DTO property names so Dapper maps automatically.
            var columns = new List<string>
            {
                @"t.""Id"" ",
                @"t.""Name"" ",
                @"t.""DueDate"" ",
                @"t.""Priority"" ",
                @"t.""CreatedAt"" ",
                @"t.""UpdatedAt"" ",
        
                // "t.OrderIndex"
            };

            if (query.IncludeAssignees)
            {
                // Keep user column names as Id, Name, Email so Dapper can map directly and splitOn "Id" works.
                columns.AddRange(new[]
                {
                    @"u.""Id""",
                    @"u.""Name""",
                    @"u.""Email""",
                });
            }

            sb.AppendLine($"SELECT {string.Join(", ", columns)}");
            sb.AppendLine(@"FROM ""Tasks"" t"); 

            if (query.IncludeAssignees)
            {
                sb.AppendLine(@"
                    LEFT JOIN ""UserTasks"" ut ON t.""Id"" = ut.""TaskId""
                    LEFT JOIN ""Users"" u ON ut.""UserId"" = u.""Id""");
            }
        }

        private void AppendWhereBase(StringBuilder sb)
        {
            sb.AppendLine("WHERE 1=1");
        }

        private void AppendHierarchyFilters(StringBuilder sb, DynamicParameters parameters, TaskQuery query)
        {
            if (query.WorkspaceId.HasValue)
            {
                sb.AppendLine(@"AND t.""WorkspaceId"" = @WorkspaceId");
                parameters.Add("@WorkspaceId", query.WorkspaceId);
            }

            if (query.SpaceId.HasValue)
            {
                sb.AppendLine(@"AND t.""SpaceId"" = @SpaceId");
                parameters.Add("@SpaceId", query.SpaceId);
            }

            if (query.ListId.HasValue)
            {
                sb.AppendLine(@"AND t.""ListId"" = @ListId");
                parameters.Add("@ListId", query.ListId);
            }
        }

        private void AppendUserFilters(StringBuilder sb, DynamicParameters parameters, TaskQuery query)
        {
            if (query.CreatorId.HasValue)
            {
                sb.AppendLine(@"AND t.""CreatorId"" = @CreatorId");
                parameters.Add("@CreatorId", query.CreatorId);
            }

            if (query.AssigneeId.HasValue)
            {
                sb.AppendLine(@"AND EXISTS (SELECT 1 FROM ""UserTasks"" ut WHERE ut.""TaskId"" = t.""Id"" AND ut.""UserId"" = @AssigneeId)");
                parameters.Add("@AssigneeId", query.AssigneeId);
            }
        }

        private void AppendPriorityFilters(StringBuilder sb, DynamicParameters parameters, TaskQuery query)
        {
            if (query.Priority.HasValue)
            {
                sb.AppendLine(@"AND t.""Priority"" = @Priority");
                parameters.Add("@Priority", query.Priority);
            }

            if (query.Priorities?.Any() == true)
            {
                sb.AppendLine(@"AND t.""Priority"" = ANY(@Priorities)");
                parameters.Add("@Priorities", query.Priorities.Cast<int>().ToArray());
            }
        }

        private void AppendDateFilters(StringBuilder sb, DynamicParameters parameters, TaskQuery query)
        {
            if (query.DueDateBefore.HasValue)
            {
                sb.AppendLine(@"AND t.""DueDate"" <= @DueDateBefore");
                parameters.Add("@DueDateBefore", query.DueDateBefore);
            }

            if (query.IsOverdue == true)
            {
                sb.AppendLine(@"AND t.""DueDate"" < @Now AND t.""DueDate"" IS NOT NULL");
                parameters.Add("@Now", DateTime.UtcNow);
            }
        }

        private void AppendArchiveAndSearch(StringBuilder sb, DynamicParameters parameters, TaskQuery query)
        {
            sb.AppendLine(@"AND t.""IsArchived"" = @IsArchived");
            parameters.Add("@IsArchived", query.IsArchived ?? false);

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                sb.AppendLine(@"AND (t.""Name"" ILIKE @SearchTerm OR t.""Description"" ILIKE @SearchTerm)");
                parameters.Add("@SearchTerm", $"%{query.SearchTerm}%");
            }
        }

        private void AppendCursorPagination(StringBuilder sb, DynamicParameters parameters, TaskQuery query)
        {
            if (string.IsNullOrEmpty(query.Cursor))
                return;

            // Decode raw string + id from your CursorHelper
            var decoded = CursorHelper.DecodeCursor(query.Cursor); // returns (string? Value, Guid Id)

            // Convert raw string into proper CLR type suitable for SQL parameter
            object? cursorValue = ParseCursorValue(query.SortBy, decoded.Value);

            var comparison = query.Direction == SortDirection.Asc ? ">" : "<";
            var column = GetSqlColumnName(query.SortBy);

            // Composite predicate ensures stable paging when primary sort values are duplicated
            sb.AppendLine($@"
                AND (
                    t.{column} {comparison} @CursorValue
                    OR (t.{column} = @CursorValue AND t.""Id"" {comparison} @CursorId)
                )");

            parameters.Add("@CursorValue", cursorValue ?? DBNull.Value);
            parameters.Add("@CursorId", decoded.Id);
        }

        private void AppendOrderAndLimit(StringBuilder sb, DynamicParameters parameters, TaskQuery query)
        {
            // Deterministic ordering: primary column then id tie-breaker
            var sqlDirection = ToSqlDirection(query.Direction);
            sb.AppendLine($"ORDER BY t.{GetSqlColumnName(query.SortBy)} {sqlDirection}, t.\"Id\" {sqlDirection}");
            sb.AppendLine("LIMIT @PageSize");
            parameters.Add("@PageSize", query.PageSize + 1);
        }

        #endregion

        #region Helpers

        private TaskQuery ResolveUserContextFilters(TaskQuery query)
        {
            var currentUserId = _currentUserService.CurrentUserId();

            return query with
            {
                CreatorId = query.CreatedByMe == true ? currentUserId : query.CreatorId,
                AssigneeId = query.AssignedToMe == true ? currentUserId : query.AssigneeId
            };
        }

        private string GetSqlColumnName(TaskSortBy sortBy) => sortBy switch
        {
            TaskSortBy.CreatedAt => "\"CreatedAt\"",
            TaskSortBy.UpdatedAt => "\"UpdatedAt\"",
            TaskSortBy.DueDate => "\"DueDate\"",
            TaskSortBy.Priority => "\"Priority\"",
            TaskSortBy.Name => "\"Name\"",
            _ => "\"CreatedAt\""
        };

        private static string ToSqlDirection(SortDirection dir) => dir == SortDirection.Asc ? "ASC" : "DESC";

        // Local parsing helper — uses your existing CursorHelper's parsers
        private object? ParseCursorValue(TaskSortBy sortBy, string? rawValue)
        {
            return sortBy switch
            {
                TaskSortBy.CreatedAt => (object?)CursorHelper.ParseDateTimeOrNull(rawValue),
                TaskSortBy.UpdatedAt => (object?)CursorHelper.ParseDateTimeOrNull(rawValue),
                TaskSortBy.DueDate => (object?)CursorHelper.ParseDateTimeOrNull(rawValue),
                TaskSortBy.Priority => rawValue == null ? (int?)null : (object?)CursorHelper.ParseInt(rawValue),
                TaskSortBy.Name => (object?)(rawValue ?? string.Empty),
                _ => (object?)CursorHelper.ParseDateTimeOrNull(rawValue)
            };
        }

        private string GenerateNextCursor(TaskSummaryBuilder item, TaskSortBy sortBy)
        {
            // Use your generic CursorHelper.GenerateCursor with a value selector and id selector
            return CursorHelper.GenerateCursor(
                item,
                t => sortBy switch
                {
                    TaskSortBy.CreatedAt => (object?)t.CreatedAt,
                    TaskSortBy.UpdatedAt => (object?)t.UpdatedAt ?? t.CreatedAt,
                    TaskSortBy.DueDate => (object?)t.DueDate,
                    TaskSortBy.Priority => (object?)((int)t.Priority),
                    TaskSortBy.Name => (object?)t.Name ?? string.Empty,
                    _ => (object?)t.CreatedAt
                },
                t => t.Id);
        }

        #endregion

        #region Builder class

        // Kept inside handler for minimal file changes; extract to a small file if preferred.
        private class TaskSummaryBuilder
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public DateTime? DueDate { get; set; }
            public Priority Priority { get; set; }
            public List<UserSummary> Assignees { get; set; } = new();
            public HashSet<Guid> AssigneeIds { get; set; } = new();
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            // public int OrderIndex { get; set; }
        }

        #endregion
    }
}
