using MediatR;
using src.Helper.Results;

namespace src.Feature.TaskManager.UpdateTask;

public record class UpdateTaskRequest(
    Guid Id,
    string? Name,
    string? Description,
    int? Priority,
    DateTime? StartDate,
    DateTime? DueDate,
    long? TimeEstimate,
    long? TimeSpent,
    int? OrderIndex,
    bool? IsArchived,
    bool? IsPrivate,
    Guid? ListId) : IRequest<Result<UpdateTaskResponse, ErrorResponse>>;

public record class UpdateTaskBodyRequest(
    string? Name,
    string? Description,
    int? Priority,
    DateTime? StartDate,
    DateTime? DueDate,
    long? TimeEstimate,
    long? TimeSpent,
    int? OrderIndex,
    bool? IsArchived,
    bool? IsPrivate,
    Guid? ListId
);
