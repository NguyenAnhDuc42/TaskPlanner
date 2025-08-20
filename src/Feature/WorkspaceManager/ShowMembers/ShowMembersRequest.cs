using MediatR;
using src.Application.Common.DTOs;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager.ShowMembers;

public record class ShowMembersRequest(Guid workspaceId) : IRequest<Result<List<UserSummary>,ErrorResponse>>;

