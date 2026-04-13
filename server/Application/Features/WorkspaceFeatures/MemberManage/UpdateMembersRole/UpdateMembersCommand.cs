using Application.Common.Interfaces;
using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Application.Features.WorkspaceFeatures.MemberManage.UpdateMembersRole;

public record class UpdateMembersCommand(Guid workspaceId, List<UpdateMemberValue> members) : ICommandRequest<Guid>;

public record class UpdateMemberValue(Guid userId, Role? role, MembershipStatus? status);