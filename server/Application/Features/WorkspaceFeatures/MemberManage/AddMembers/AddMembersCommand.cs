using Application.Common.Interfaces;
using Domain.Enums;

namespace Application.Features.WorkspaceFeatures.MemberManage.AddMembers;

public record AddMembersCommand(
    Guid workspaceId, 
    List<MemberValue> members, 
    bool? enableEmail, 
    string? message
) : ICommandRequest, IAuthorizedWorkspaceRequest;

public record MemberValue(string email, Role role);
