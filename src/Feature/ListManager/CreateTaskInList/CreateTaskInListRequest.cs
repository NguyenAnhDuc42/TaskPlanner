using MediatR;
using src.Domain.Entities.WorkspaceEntity.SupportEntiy;
using src.Domain.Enums;
using src.Feature.TaskManager.CreateTask;
using src.Helper.Results;

namespace src.Feature.ListManager.CreateTaskInList;

public record class CreateTaskInListRequest(string name, string description, Priority priority, DateTime? startDate, DateTime? dueDate, bool isPrivate,Guid listId) : IRequest<Result<CreateTaskResponse, ErrorResponse>>;


