using System;
using System.Collections.Generic;

namespace Application;

public record GetSpaceDocumentsQuery(Guid SpaceId) : IQueryRequest<List<SpaceDocumentRecord>>, IAuthorizedWorkspaceRequest;

public record SpaceDocumentRecord(
    Guid Id,
    string Name,
    bool IsDefault
);
