using System;
using System.Numerics;
using MediatR;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Data;

namespace src.Feature.TaskManager.DeleteTask;

public class DeleteTaskHandler : IRequestHandler<DeleteTaskRequest, Result<DeleteTaskResponse, ErrorResponse>>
{
    private readonly PlannerDbContext _context;
    private readonly IHierarchyRepository _hierarchyRepository;
    public DeleteTaskHandler(PlannerDbContext context, IHierarchyRepository hierarchyRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
    }
    public async Task<Result<DeleteTaskResponse, ErrorResponse>> Handle(DeleteTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await _hierarchyRepository.GetPlanTaskByIdAsync(request.id, cancellationToken);
        if (task == null) return Result<DeleteTaskResponse, ErrorResponse>.Failure(ErrorResponse.NotFound("Task not found"));

        _context.Remove(task);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<DeleteTaskResponse, ErrorResponse>.Success(new DeleteTaskResponse("Task deleted successfully"));

    }
}
