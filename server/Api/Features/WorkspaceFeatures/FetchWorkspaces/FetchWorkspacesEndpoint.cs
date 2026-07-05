using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class FetchWorkspacesEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
       
        app.MapGet("/api/workspaces/sync", async (
            [FromQuery] string? cursor,
            [FromQuery] string? name,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool? owned = null,
            [FromQuery] bool? isArchived = null,
            [FromQuery] SortDirection direction = SortDirection.Ascending) =>
        {
            var pagination = new CursorPaginationRequest(cursor, pageSize == 0 ? 10 : pageSize, Direction: direction);
            var filter = new WorkspaceFilter(name, owned, isArchived);
            var query = new FetchWorkspacesQuery(pagination, filter);

            var result = await dispatcher.QueryAsync<FetchWorkspacesQuery, PagedResult<WorkspaceSnippetRecord>>(query, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MinimalResultExtensions.Problem(result.Error!);
        })
        .RequireAuthorization()
        .WithTags("WorkspacesSync");
    }
}
