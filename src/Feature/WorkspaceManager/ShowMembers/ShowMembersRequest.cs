using MediatR;
using src.Contract;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager.ShowMembers;

public record class ShowMembersRequest(Guid workspaceId) : IRequest<Result<List<UserSummary>,ErrorResponse>>;

