using MediatR;
using src.Helper.Results;

namespace src.Feature.TaskManager.DeleteTask;

public record class DeleteTaskRequest(Guid id) : IRequest<Result<DeleteTaskResponse, ErrorResponse>>;


