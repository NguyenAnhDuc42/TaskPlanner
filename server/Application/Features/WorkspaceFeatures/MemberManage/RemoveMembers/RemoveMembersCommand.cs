using Application.Common.Interfaces;

namespace Application.Features.WorkspaceFeatures;

public record RemoveMembersCommand(
    Guid workspaceId, 
    List<Guid> memberIds
) : ICommandRequest<Guid>, IAuthorizedWorkspaceRequest;