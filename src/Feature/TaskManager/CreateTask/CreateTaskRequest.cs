using MediatR;
using src.Domain.Entities.WorkspaceEntity.SupportEntiy;
using src.Helper.Results;

namespace src.Feature.TaskManager.CreateTask;

public record class CreateTaskRequest(string name, string description, int priority,PlanTaskStatus? status, DateTime? startDate, DateTime? dueDate, bool isPrivate,  Guid workspaceId, Guid spaceId, Guid? folderId, Guid listId) : IRequest<Result<CreateTaskResponse, ErrorResponse>>;

