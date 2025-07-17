using System;
using MediatR;
using src.Domain.Entities.WorkspaceEntity;
using src.Domain.Entities.WorkspaceEntity.SupportEntiy;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Data;

namespace src.Feature.TaskManager.CreateTask;

public class CreateTaskHandler : IRequestHandler<CreateTaskRequest, Result<CreateTaskResponse, ErrorResponse>>
{
    private readonly PlannerDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    public CreateTaskHandler(PlannerDbContext context, ICurrentUserService currentUserService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }
    public async Task<Result<CreateTaskResponse, ErrorResponse>> Handle(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.CurrentUserId();
        var status = request.status ?? PlanTaskStatus.ToDo;

        var task = PlanTask.Create(request.name, request.description, request.priority,status, request.startDate, request.dueDate, request.isPrivate, request.workspaceId, request.spaceId, request.folderId, request.listId, userId);

        try
        {
            await _context.AddAsync(task);
            await _context.SaveChangesAsync();
            return Result<CreateTaskResponse, ErrorResponse>.Success(new CreateTaskResponse(task.Id, "Task created successfully"));
        }
        catch (Exception ex)
        {
            return Result<CreateTaskResponse, ErrorResponse>.Failure(ErrorResponse.Internal(ex.Message));
        }
    }
}
