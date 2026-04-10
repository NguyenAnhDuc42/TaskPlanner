using Application.Common.Filters;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Enums;

namespace Application.Features.WorkspaceFeatures.MemberManage.GetMembers;

public record class GetMembersQuery(CursorPaginationRequest pagination, Guid WorkspaceId, GetMembersFilter filter) : IQueryRequest<PagedResult<MemberDto>>;

public record class GetMembersFilter(
    string? Name,
    string? Email,
    Guid? SpaceId,
    Guid? TaskId,
    Role? Role
);

public record class MemberDto
{
    public Guid Id { get; init; }
    public Guid WorkspaceMemberId { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? AvatarUrl { get; init; }
    public Role Role { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? JoinedAt { get; init; }
}