using MediatR;
using src.Helper.Results;

namespace src.Feature.ListManager.GetListInfo;

public record class GetListInfoRequest(Guid listId) : IRequest<Result<TaskLineList, ErrorResponse>>;