using MediatR;
using src.Feature.TaskManager.CreateTask;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Data;
using src.Domain.Entities.WorkspaceEntity;
using src.Domain.Entities.WorkspaceEntity.SupportEntiy;
namespace src.Feature.ListManager.CreateTaskInList;

public class CreateTaskInListHandler : IRequestHandler<CreateTaskInListRequest, Result<CreateTaskResponse, ErrorResponse>>
{
    private readonly PlannerDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateTaskInListHandler(PlannerDbContext context, ICurrentUserService currentUserService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<CreateTaskResponse, ErrorResponse>> Handle(CreateTaskInListRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.CurrentUserId();
        var status = request.status ?? PlanTaskStatus.ToDo;

        // Fetch the list to get associated workspace, space, and folder IDs
        var list = await _context.Lists.FindAsync(request.listId, cancellationToken);
        if (list == null)
        {
            return Result<CreateTaskResponse, ErrorResponse>.Failure(ErrorResponse.NotFound("List not found"));
        }

        var task = PlanTask.Create(
            request.name,
            request.description,
            request.priority,
            status,
            request.startDate,
            request.dueDate,
            request.isPrivate,
            list.WorkspaceId,
            list.SpaceId,
            list.FolderId,
            request.listId,
            userId
        );

        await _context.Tasks.AddAsync(task,cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<CreateTaskResponse, ErrorResponse>.Success(new CreateTaskResponse(task.Id, "Task created successfully in list"));
    }
}


