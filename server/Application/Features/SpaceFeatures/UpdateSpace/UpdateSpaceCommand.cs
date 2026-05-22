using System;
namespace Application;

public record UpdateSpaceCommand(
    Guid SpaceId,
    string? Name,
    string? Color,
    string? Icon,
    bool? IsPrivate
) : ICommandRequest, IAuthorizedWorkspaceRequest;

