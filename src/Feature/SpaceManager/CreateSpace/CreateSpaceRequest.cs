using MediatR;
using src.Helper.Results;

namespace src.Feature.SpaceManager.CreateSpace;

public record class CreateSpaceRequest(Guid workspaceId,string name,string icon,string color) : IRequest<Result<CreateSpaceResponse, ErrorResponse>>;


