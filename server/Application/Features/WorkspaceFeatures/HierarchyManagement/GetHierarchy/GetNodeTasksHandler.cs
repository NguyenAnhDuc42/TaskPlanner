using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;
using Dapper;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public class GetNodeTasksHandler(IDataBase db) : IQueryHandler<GetNodeTasksQuery, NodeTasksDto>
{
    public async Task<Result<NodeTasksDto>> Handle(GetNodeTasksQuery request, CancellationToken ct)
    {
        
        var rawTasks = (await db.QueryAsync<TaskRawItem>(GetHierarchySql.TasksQuery, new
        {
            WorkspaceId = request.WorkspaceId,
            ParentId = request.ParentId,
            ParentType = request.ParentType,
            CursorOrderKey = request.CursorOrderKey,
            CursorTaskId = request.CursorTaskId,
            PageSize = request.PageSize + 1
        }, cancellationToken: ct)).AsList();

        var hasMore = rawTasks.Count > request.PageSize;
        if (hasMore) rawTasks.RemoveAt(rawTasks.Count - 1);

        var tasks = new List<TaskHierarchyDto>(rawTasks.Count);
        foreach (var t in rawTasks)
        {
            tasks.Add(new TaskHierarchyDto
            {
                Id = t.Id,
                Name = t.Name,
                StatusId = t.StatusId,
                Priority = t.Priority,
                OrderKey = t.OrderKey,
                Color = "",
                Icon = ""
            });
        }

        var last = rawTasks.LastOrDefault();

        return Result<NodeTasksDto>.Success(new NodeTasksDto
        {
            Tasks = tasks,
            HasMore = hasMore,
            NextCursorOrderKey = hasMore ? last?.OrderKey : null,
            NextCursorTaskId = hasMore ? last?.Id.ToString() : null
        });
    }

    private record TaskRawItem
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public Guid? StatusId { get; init; }
        public Priority Priority { get; init; }
        public string? OrderKey { get; init; }
        public string ParentType { get; init; } = null!;
    }
}
