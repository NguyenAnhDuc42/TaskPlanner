using MediatR;
using src.Application.Common.DTOs;
using src.Helper.Results;

namespace src.Feature.SpaceManager.GetSpaceInfo;

public record class GetSpaceInfoRequest(Guid spaceId) : IRequest<Result<List<TaskSummary>, ErrorResponse>>;

