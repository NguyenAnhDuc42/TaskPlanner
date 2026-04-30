using Application.Common.Interfaces;

namespace Application.Features.SpaceFeatures;

public record CreateSpaceCommand(
    string name,
    string? description,
    string color,
    string icon,
    bool isPrivate,
    List<Guid>? memberIdsToInvite = null
) : ICommandRequest<Guid>, IAuthorizedWorkspaceRequest;