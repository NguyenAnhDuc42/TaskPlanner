using System;
using MediatR;
using src.Domain.Entities.WorkspaceEntity;
using Microsoft.EntityFrameworkCore;
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

        // First, verify that the parent list exists and belongs to the correct hierarchy.
        var listExists = await _context.Lists.AnyAsync(l => 
            l.Id == request.listId && 
            l.SpaceId == request.spaceId && 
            l.WorkspaceId == request.workspaceId, cancellationToken);

        if (!listExists)
        {
            return Result<CreateTaskResponse, ErrorResponse>.Failure(ErrorResponse.NotFound("Parent list not found or does not belong to the specified hierarchy."));
        }

        var task = PlanTask.Create(request.name, request.description, request.priority,s request.startDate, request.dueDate, request.isPrivate, request.workspaceId, request.spaceId, request.folderId, request.listId, userId);

        await _context.Tasks.AddAsync(task,cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<CreateTaskResponse, ErrorResponse>.Success(new CreateTaskResponse(task.Id, "Task created successfully"));
    }
}
