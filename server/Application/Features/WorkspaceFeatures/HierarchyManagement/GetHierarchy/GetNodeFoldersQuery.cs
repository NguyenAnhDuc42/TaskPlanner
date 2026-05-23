using System;
using Application;

namespace Application;

public record GetNodeFoldersQuery(Guid WorkspaceId, Guid NodeId, CursorPaginationRequest Pagination) : IQueryRequest<PagedResult<FolderRecord>>, IAuthorizedWorkspaceRequest;
