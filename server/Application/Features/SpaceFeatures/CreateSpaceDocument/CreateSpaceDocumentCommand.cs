using System;

namespace Application;

public record CreateSpaceDocumentCommand(
    Guid SpaceId,
    string Name
) : ICommandRequest<SpaceDocumentRecord>, IAuthorizedWorkspaceRequest;
