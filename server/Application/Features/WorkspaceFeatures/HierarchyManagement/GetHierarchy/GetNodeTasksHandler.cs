using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Enums;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public class GetNodeTasksHandler : IQueryHandler<GetNodeTasksQuery, NodeTasksDto>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;

    public GetNodeTasksHandler(IDataBase db, ICurrentUserService currentUserService) {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<NodeTasksDto>> Handle(GetNodeTasksQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) return Result.Failure<NodeTasksDto>(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        var rawTasks = (await _db.QueryAsync<TaskRawItem>(GetHierarchySql.TasksQuery, new
        {
            WorkspaceId = request.WorkspaceId,
            ParentId = request.ParentId,
            ParentType = request.ParentType,
            CursorOrderKey = request.CursorOrderKey,
            CursorTaskId = request.CursorTaskId,
            PageSize = request.PageSize + 1  // fetch one extra to detect HasMore
        }, cancellationToken: cancellationToken)).ToList();

        var hasMore = rawTasks.Count > request.PageSize;
        if (hasMore) rawTasks.RemoveAt(rawTasks.Count - 1);

        var tasks = rawTasks.Select(t => new TaskHierarchyDto
        {
            Id = t.Id,
            Name = t.Name,
            StatusId = t.StatusId,
            Priority = t.Priority,
            OrderKey = t.OrderKey,
            Color = "",
            Icon = ""
        }).ToList();

        var last = rawTasks.LastOrDefault();

        return Result.Success(new NodeTasksDto
        {
            Tasks = tasks,
            HasMore = hasMore,
            NextCursorOrderKey = hasMore ? last?.OrderKey : null,
            NextCursorTaskId = hasMore ? last?.Id.ToString() : null
        });
    }

    private class TaskRawItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public Guid? StatusId { get; set; }
        public Priority Priority { get; set; }
        public string? OrderKey { get; set; }
        public string ParentType { get; set; } = null!;
    }
}
