using MediatR;
using src.Helper.Results;

namespace src.Feature.User.JoinWorkspace;

public record class JoinWorkspaceRequest(string joinCode) : IRequest<Result<JoinWorkspaceRespose, ErrorResponse>>;

