

using Application.Common.Filters;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Contract.UserContract;
using Domain.Enums;

namespace Application.Features.WorkspaceFeatures.MemberManage.GetMembers;

public record class GetMembersQuery(CursorPaginationRequest pagination, Guid WorkspaceId, GetMembersFilter filter) : IQuery<PagedResult<MemberDto>>;

public record class GetMembersFilter(
    string? Name,
    string? Email,
    Guid? SpaceId,
    Guid? TaskId,
    Role? Role
);
