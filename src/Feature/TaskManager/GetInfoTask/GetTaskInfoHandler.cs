using System;
using MediatR;
using src.Contract;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;



namespace src.Feature.TaskManager.GetInfoTask;

public class GetTaskInfoHandler : IRequestHandler<GetTaskInfoRequest, Result<TaskDetail, ErrorResponse>>
{
    private readonly IHierarchyRepository _hierarchyRepository;

    public GetTaskInfoHandler(IHierarchyRepository hierarchyRepository)
    {
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
    }

    public async Task<Result<TaskDetail, ErrorResponse>> Handle(GetTaskInfoRequest request, CancellationToken cancellationToken)
    {
        var task = await _hierarchyRepository.GetPlanTaskByIdAsync(request.Id, cancellationToken);

        if (task is null) return Result<TaskDetail, ErrorResponse>.Failure(ErrorResponse.NotFound("Task not found"));

        var response = new TaskDetail(
            task.Id,
            task.Name,
            task.Description,
            task.Priority,
            task.Status,
            task.DueDate,
            task.StartDate,
            task.TimeEstimate,
            task.TimeSpent,
            task.OrderIndex,
            task.IsArchived,
            task.IsPrivate,
            task.ListId,
            task.CreatorId
        );
        return Result<TaskDetail, ErrorResponse>.Success(response);
    }
}
