using MediatR;
using src.Contract;
using src.Helper.Results;

namespace src.Feature.SpaceManager.GetSpaceInfo;

public record class GetSpaceInfoRequest(Guid spaceId) : IRequest<Result<List<TaskSummary>, ErrorResponse>>;

