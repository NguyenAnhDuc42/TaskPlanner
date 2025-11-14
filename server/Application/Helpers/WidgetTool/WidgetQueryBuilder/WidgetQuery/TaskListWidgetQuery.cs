using System;
using Application.Contract.WidgetDtos;
using Application.Interfaces.Repositories;
using Dapper;
using Domain.Enums.RelationShip;

namespace Application.Helpers.WidgetTool.WidgetQueryBuilder.WidgetQuery;

public static class TaskListWidgetQuery
{
    public static async Task<List<TaskListWidgetItemDto>> ExecuteAsync(
        IUnitOfWork unitOfWork,
        Guid layerId,
        EntityLayerType layerType,
        WidgetFilter filter,
        CancellationToken cancellationToken)
    {
        var builder = new SqlBuilder();

        var selector = builder.AddTemplate(@"
            SELECT 
                t.id,
                t.title,
                t.status_id as statusId,
                t.due_date as dueDate,
                t.priority
            FROM tasks t
            INNER JOIN lists l ON t.list_id = l.id
            INNER JOIN folders f ON l.folder_id = f.id
            INNER JOIN spaces s ON f.space_id = s.id
            INNER JOIN workspaces w ON s.workspace_id = w.id
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
            case EntityLayerType.ProjectList:
                builder.Where("l.id = @layerId");
                break;
        }

        builder.Where("t.created_at >= @dateFrom");

        if (filter.StatusIds.Any())
            builder.Where("t.status_id = ANY(@statusIds)");

        if (!string.IsNullOrEmpty(filter.SearchText))
            builder.Where("t.title ILIKE @searchText");

        if (filter.DateTo.HasValue)
            builder.Where("t.due_date <= @dateToFilter");

        var parameters = new DynamicParameters();
        parameters.Add("@layerId", layerId);
        parameters.Add("@dateFrom", filter.DateFrom ?? DateTime.MinValue);
        parameters.Add("@dateToFilter", filter.DateTo);
        parameters.Add("@searchText", filter.SearchText == null ? null : $"%{filter.SearchText}%");
        parameters.Add("@statusIds", filter.StatusIds.Any() ? filter.StatusIds.ToArray() : null);
        parameters.Add("@limit", filter.Limit ?? 1000);

        var tasks = await unitOfWork.QueryAsync<TaskListWidgetItemDto>(selector.RawSql, parameters, cancellationToken);

        return tasks.ToList();
    }
}
