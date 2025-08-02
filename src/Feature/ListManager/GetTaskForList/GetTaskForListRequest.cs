using MediatR;
using src.Contract;
using src.Helper.Results;

namespace src.Feature.ListManager.GetTaskForList;

public record class GetTaskForListRequest(Guid listId) : IRequest<Result<List<StatusColumn>, ErrorResponse>>;
