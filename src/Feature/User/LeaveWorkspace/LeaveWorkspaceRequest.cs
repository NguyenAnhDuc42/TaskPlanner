using MediatR;
using src.Helper.Results;

namespace src.Feature.User.LeaveWorkspace;

public record class LeaveWorkspaceRequest(Guid workspaceId) : IRequest<Result<string, ErrorResponse>>;

