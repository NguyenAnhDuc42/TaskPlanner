using MediatR;
using src.Domain.Entities.WorkspaceEntity.SupportEntiy;
using src.Feature.TaskManager.CreateTask;
using src.Helper.Results;

namespace src.Feature.ListManager.CreateTaskInList;

public record class CreateTaskInListRequest(string name, string description, int priority,PlanTaskStatus? status, DateTime? startDate, DateTime? dueDate, bool isPrivate,Guid listId) : IRequest<Result<CreateTaskResponse, ErrorResponse>>;


