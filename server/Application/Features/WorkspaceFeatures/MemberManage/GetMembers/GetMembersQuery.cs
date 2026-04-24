using Application.Common.Filters;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Enums;

namespace Application.Features.WorkspaceFeatures;

public record class GetMembersQuery(
    CursorPaginationRequest pagination, 
    Guid WorkspaceId, 
    GetMembersFilter filter
) : IQueryRequest<PagedResult<MemberDto>>, IAuthorizedWorkspaceRequest;

public record class GetMembersFilter(
    string? Name,
    string? Email,
    Guid? SpaceId,
    Guid? TaskId,
    Role? Role
);

public record class MemberDto(
    Guid Id,
    Guid WorkspaceMemberId,
    string? Name,
    string? Email,
    string? AvatarUrl,
    Role Role,
    DateTimeOffset CreatedAt,
    DateTimeOffset? JoinedAt
);