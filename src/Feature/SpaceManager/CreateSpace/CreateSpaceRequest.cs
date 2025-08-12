
using MediatR;

namespace src.Feature.SpaceManager.CreateSpace;

public record class CreateSpaceRequest(Guid workspaceId,CreateSpaceBody body) : IRequest<Guid>;

public record CreateSpaceBody(string name,string icon,string color);


