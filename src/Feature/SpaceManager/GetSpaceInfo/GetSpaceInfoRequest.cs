using MediatR;
using src.Helper.Results;

namespace src.Feature.SpaceManager.GetSpaceInfo;

public record class GetSpaceInfoRequest(Guid spaceId) : IRequest<Result<TaskList, ErrorResponse>>;

