using MediatR;
using src.Contract;
using src.Helper.Results;

namespace src.Feature.User.GetUserWorkspace;

public record class GetUserWorkspaceRequest() : IRequest<Result<List<WorkspaceDetail>,ErrorResponse>>;
