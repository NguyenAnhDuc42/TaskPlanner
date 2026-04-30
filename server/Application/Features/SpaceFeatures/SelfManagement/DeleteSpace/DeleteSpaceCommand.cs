using Application.Common.Interfaces;

namespace Application.Features.SpaceFeatures;

public record DeleteSpaceCommand(
    Guid SpaceId
) : ICommandRequest, IAuthorizedWorkspaceRequest;
