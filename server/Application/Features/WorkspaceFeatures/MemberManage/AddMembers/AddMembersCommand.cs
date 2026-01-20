using Application.Common.Interfaces;
using Domain.Enums;

namespace Application.Features.WorkspaceFeatures.MemberManage.AddMembers;

public record class AddMembersCommand(Guid workspaceId, List<MemberValue> members,bool? enableEmail,string? message) : ICommand<Guid>;

public record class MemberValue(string email, Role role);

