using Application.Common.Interfaces;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.WorkspaceFeatures.MemberManage.UpdateMembers;

public record class UpdateMembersCommand(Guid workspaceId, List<UpdateMemberValue> members) : ICommand<Unit>;


public record class UpdateMemberValue(Guid userId,Role? role, MembershipStatus? status);