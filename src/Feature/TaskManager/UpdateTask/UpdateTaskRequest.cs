using MediatR;
using src.Domain.Entities.WorkspaceEntity.SupportEntiy;
using src.Domain.Enums;
using src.Helper.Results;

namespace src.Feature.TaskManager.UpdateTask;

public record class UpdateTaskRequest(
    Guid Id,
    string? Name, 
    string? Description,
    Priority? Priority,
    DateTime? StartDate,
    DateTime? DueDate,
    long? TimeEstimate,
    long? TimeSpent,
    int? OrderIndex,
    bool? IsArchived,
    bool? IsPrivate) : IRequest<Result<UpdateTaskResponse, ErrorResponse>>;

public record class UpdateTaskBodyRequest(
    string? Name,
    string? Description,
    Priority? Priority,
    DateTime? StartDate,
    DateTime? DueDate,

    long? TimeEstimate,
    long? TimeSpent,
    int? OrderIndex,
    bool? IsArchived,
    bool? IsPrivate
);
