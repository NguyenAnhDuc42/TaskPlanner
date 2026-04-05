using Application.Interfaces.Repositories;
using Application.Helpers;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public class GetNodeTasksHandler : BaseFeatureHandler, IRequestHandler<GetNodeTasksQuery, NodeTasksDto>
{
    public GetNodeTasksHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<NodeTasksDto> Handle(GetNodeTasksQuery request, CancellationToken cancellationToken)
    {
        var rawTasks = (await UnitOfWork.QueryAsync<TaskRawItem>(GetHierarchySql.TasksQuery, new
        {
            WorkspaceId = request.WorkspaceId,
            ParentId = request.ParentId,
            ParentType = request.ParentType,
            CursorOrderKey = request.CursorOrderKey,
            CursorTaskId = request.CursorTaskId,
            PageSize = request.PageSize + 1  // fetch one extra to detect HasMore
        }, cancellationToken)).ToList();

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

        return new NodeTasksDto
        {
            Tasks = tasks,
            HasMore = hasMore,
            NextCursorOrderKey = hasMore ? last?.OrderKey : null,
            NextCursorTaskId = hasMore ? last?.Id.ToString() : null
        };
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
