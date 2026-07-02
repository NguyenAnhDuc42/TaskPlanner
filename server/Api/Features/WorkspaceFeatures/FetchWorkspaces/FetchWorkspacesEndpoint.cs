using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class FetchWorkspacesEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        // GET, not the bare "/api/workspaces" — that path is already owned by the legacy
        // WorkspacesController (MVC routing), and Endpoint Routing can't disambiguate two
        // handlers mapped to the identical verb+path. Same /sync-suffix coexistence pattern
        // as every other slice in this migration.
        app.MapGet("/api/workspaces/sync", async (
            [FromQuery] string? cursor,
            [FromQuery] string? name,
            [FromQuery] int pageSize,
            [FromQuery] bool? owned,
            [FromQuery] bool? isArchived,
            [FromQuery] SortDirection direction,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var pagination = new CursorPaginationRequest(cursor, pageSize == 0 ? 10 : pageSize, Direction: direction);
            var filter = new WorkspaceFilter(name, owned, isArchived);
            var query = new FetchWorkspacesQuery(pagination, filter);

            var result = await dispatcher.QueryAsync<FetchWorkspacesQuery, PagedResult<WorkspaceSnippetRecord>>(query, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        })
        .RequireAuthorization()
        .WithTags("WorkspacesSync");
    }
}
