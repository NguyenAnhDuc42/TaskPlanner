using System;
using Application;

namespace Application;

public record GetNodeSpacesQuery(Guid WorkspaceId, CursorPaginationRequest Pagination) : IQueryRequest<PagedResult<SpaceRecord>>, IAuthorizedWorkspaceRequest;
