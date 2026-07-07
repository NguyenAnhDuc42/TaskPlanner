namespace Api;

// Workspace is a read-replica entity — it bypasses Bootstrap/Delta entirely (see
// FRONTEND_SYNC_CONTEXT.md §1), so unlike Task/Space/Folder there's no other way for the
// client to learn its workspace list. NOT IAuthorizedWorkspaceRequest — this lists
// workspaces across the user's whole account, before any single workspace is selected.
public record FetchWorkspacesQuery(
    CursorPaginationRequest Pagination,
    WorkspaceFilter Filter
) : IQueryRequest<PagedResult<WorkspaceRecord>>;
