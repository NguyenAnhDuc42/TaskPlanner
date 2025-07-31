using MediatR;
using src.Helper.Results;

namespace src.Feature.User.CreateWorkspace;

public record class CreateWorkspaceRequest(
     string Name,
    string Description,
    string Color,
    string Icon,
    bool IsPrivate
) : IRequest<Result<string, ErrorResponse>>;