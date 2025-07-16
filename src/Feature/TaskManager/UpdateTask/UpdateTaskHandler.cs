using System;
using MediatR;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Data;

namespace src.Feature.TaskManager.UpdateTask;

public class UpdateTaskHandler : IRequestHandler<UpdateTaskRequest, Result<UpdateTaskResponse, ErrorResponse>>
{
    private readonly PlannerDbContext _context;
    private readonly IHierarchyRepository _hierarchyRepository;

    public UpdateTaskHandler(PlannerDbContext context, IHierarchyRepository hierarchyRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
    }

    public async Task<Result<UpdateTaskResponse, ErrorResponse>> Handle(UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await _hierarchyRepository.GetPlanTaskByIdAsync(request.Id, cancellationToken);
        if (task == null) return Result<UpdateTaskResponse, ErrorResponse>.Failure(ErrorResponse.NotFound("Task not found"));

        task.Name = request.Name ?? task.Name;
        task.Description = request.Description ?? task.Description;
        task.Priority = request.Priority ?? task.Priority;
        task.StartDate = request.StartDate ?? task.StartDate;
        task.DueDate = request.DueDate ?? task.DueDate;
        task.TimeEstimate = request.TimeEstimate ?? task.TimeEstimate;
        task.TimeSpent = request.TimeSpent ?? task.TimeSpent;
        task.OrderIndex = request.OrderIndex ?? task.OrderIndex;
        task.IsArchived = request.IsArchived ?? task.IsArchived;
        task.IsPrivate = request.IsPrivate ?? task.IsPrivate;
        task.ListId = request.ListId ?? task.ListId;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            var response = new Task(
                task.Id,
                task.Name,
                task.Description,
                task.Priority,
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
            return Result<UpdateTaskResponse, ErrorResponse>.Success(new UpdateTaskResponse(response, "Task updated successfully"));
        }
        catch (Exception ex)
        {
            return Result<UpdateTaskResponse, ErrorResponse>.Failure(ErrorResponse.Internal(ex.Message));
        }
    }
}
