using Application.Common.Interfaces;

namespace Application.Features.SpaceFeatures.SelfManagement.DeleteSpace;

public record DeleteSpaceCommand(
    Guid workspaceId,
    Guid SpaceId
) : ICommandRequest, IAuthorizedWorkspaceRequest;
