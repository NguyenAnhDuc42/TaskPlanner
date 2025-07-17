using System;
using MediatR;
using src.Domain.Entities.WorkspaceEntity;
using src.Domain.Entities.WorkspaceEntity.SupportEntiy;
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

        task.Update(request.Name, request.Description, request.Priority ?? 0,request.Status ?? PlanTaskStatus.ToDo,request.StartDate, request.DueDate, request.IsPrivate ?? false);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
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
            return Result<UpdateTaskResponse, ErrorResponse>.Success(new UpdateTaskResponse(response, "Task updated successfully"));
        }
        catch (Exception ex)
        {
            return Result<UpdateTaskResponse, ErrorResponse>.Failure(ErrorResponse.Internal(ex.Message));
        }
    }
}
