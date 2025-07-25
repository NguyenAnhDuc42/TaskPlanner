using MediatR;
using src.Helper.Results;

namespace src.Feature.Workspace.CreateWorkspace;

public record CreateWorkspaceRequest(
    string Name,
    string Description,
    string Color,
    string Icon,
    bool IsPrivate
) : IRequest<Result<CreateWorkspaceResponse, ErrorResponse>>;
