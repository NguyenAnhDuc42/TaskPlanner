using MediatR;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager.CreateSpace;

public record class CreateSpaceRequest(Guid workspaceId,CreateSpaceBody body) : IRequest<Result<string, ErrorResponse>>;

public record CreateSpaceBody(string name,string icon,string color);
