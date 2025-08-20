using System;
using MediatR;
using src.Application.Common.DTOs;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Services.Query;

namespace src.Feature.TaskManager.GetTasks;

public class GetTasksHandler : IRequestHandler<GetTasksRequest, PagedResult<TasksSummary>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly TasksQueryService _tasksQueryService;
    public GetTasksHandler(ICurrentUserService currentUserService, TasksQueryService tasksQueryService)
    {
        _currentUserService = currentUserService;
        _tasksQueryService = tasksQueryService;
    }

    public async Task<PagedResult<TasksSummary>> Handle(GetTasksRequest request, CancellationToken cancellationToken)
    {
         return await _tasksQueryService.GetTasksAsync(request.query, _currentUserService.CurrentUserId(), cancellationToken);
    }
}
