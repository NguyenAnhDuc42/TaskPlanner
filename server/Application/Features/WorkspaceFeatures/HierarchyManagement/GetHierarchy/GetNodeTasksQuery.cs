using System;
using Application;

namespace Application;

public record GetNodeTasksQuery(Guid WorkspaceId, Guid ParentId, string ParentType, CursorPaginationRequest Pagination) : IQueryRequest<PagedResult<TaskRecord>>, IAuthorizedWorkspaceRequest;
