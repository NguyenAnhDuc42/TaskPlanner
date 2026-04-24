using Application.Common.Interfaces;

namespace Application.Features.SpaceFeatures;

public record DeleteSpaceCommand(
    Guid workspaceId,
    Guid SpaceId
) : ICommandRequest, IAuthorizedWorkspaceRequest;
