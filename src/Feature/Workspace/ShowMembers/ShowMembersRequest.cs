using MediatR;
using src.Helper.Results;

namespace src.Feature.Workspace.ShowMembers;

public record class ShowMembersRequest(Guid workspaceId) : IRequest<Result<Members,ErrorResponse>>;

