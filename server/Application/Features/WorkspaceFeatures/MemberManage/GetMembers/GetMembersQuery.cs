using Application.Contract.UserContract;
using Domain.Enums;
using MediatR;

namespace Application.Features.WorkspaceFeatures.MemberManage.GetMembers;

public record class GetMembersQuery(Guid WorkspaceId) : IRequest<List<MemberDto>>;

public record class GetMembersFilter(
    Guid WorkspaceId,
    string? Name,
    string? Email,
    Guid? SpaceId,
    Guid? TaskId,
    Role? Role
) : IRequest<List<MemberDto>>;
