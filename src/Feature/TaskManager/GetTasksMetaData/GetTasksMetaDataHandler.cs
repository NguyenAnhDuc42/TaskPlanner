using System;
using MediatR;
using src.Contract;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Services.Query;

namespace src.Feature.TaskManager.GetTasksMetaData;

public class GetTasksMetaDataHandler : IRequestHandler<GetTasksMetaDataRequest, TasksMetadata>
{
    private readonly TasksQueryService _tasksQueryService;
    private readonly ICurrentUserService _currentUserService;
    public GetTasksMetaDataHandler(TasksQueryService tasksQueryService, ICurrentUserService currentUserService)
    {
        _tasksQueryService = tasksQueryService;
        _currentUserService = currentUserService;
    }
    public async Task<TasksMetadata> Handle(GetTasksMetaDataRequest request, CancellationToken cancellationToken)
    {
        return await _tasksQueryService.GetTasksMetadataAsync(request.query, _currentUserService.CurrentUserId(),cancellationToken);
    }
}
