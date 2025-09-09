using Application.Contract.SpaceContract;
using MediatR;

namespace Application.Features.SpaceFeatures.UpdateSpaceBasicInfo;

public record class UpdateSpaceBasicInfoCommand(Guid spaceId,string name,string? description) : IRequest<SpaceSummary>;
