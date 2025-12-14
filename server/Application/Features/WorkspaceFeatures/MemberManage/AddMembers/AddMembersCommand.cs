using Application.Common.Interfaces;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.WorkspaceFeatures.MemberManage.AddMembers;

public record class AddMembersCommand(Guid workspaceId, List<MemberSpec> members) : ICommand<Unit>;



public record class MemberSpec(string email, Role role, MembershipStatus status, string? joinMethod);

