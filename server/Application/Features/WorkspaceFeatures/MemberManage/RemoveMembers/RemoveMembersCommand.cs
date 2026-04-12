using Application.Common.Interfaces;

namespace Application.Features.WorkspaceFeatures.MemberManage.RemoveMembers;

public record RemoveMembersCommand(
    Guid workspaceId, 
    List<Guid> memberIds
) : ICommandRequest<Guid>, IAuthorizedWorkspaceRequest;