using System;
using MediatR;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using System.Threading;
using System.Threading.Tasks;

namespace src.Feature.TaskManager.GetInfoTask;

public class GetTaskInfoHandler : IRequestHandler<GetTaskInfoRequest, Result<Task, ErrorResponse>>
{
    private readonly IHierarchyRepository _hierarchyRepository;

    public GetTaskInfoHandler(IHierarchyRepository hierarchyRepository)
    {
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
    }

    public async Task<Result<Task, ErrorResponse>> Handle(GetTaskInfoRequest request, CancellationToken cancellationToken)
    {
        var task = await _hierarchyRepository.GetPlanTaskByIdAsync(request.Id, cancellationToken);

        if (task is null) return Result<Task, ErrorResponse>.Failure(ErrorResponse.NotFound("Task not found"));

        var response = new Task(
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
        return Result<Task, ErrorResponse>.Success(response);
    }
}
