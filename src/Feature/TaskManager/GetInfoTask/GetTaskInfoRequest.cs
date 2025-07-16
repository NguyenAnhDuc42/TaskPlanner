using MediatR;
using src.Helper.Results;

namespace src.Feature.TaskManager.GetInfoTask;

public record class GetTaskInfoRequest(Guid Id) : IRequest<Result<Task, ErrorResponse>>;
