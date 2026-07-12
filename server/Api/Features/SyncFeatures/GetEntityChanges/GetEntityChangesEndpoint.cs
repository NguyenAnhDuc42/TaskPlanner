using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class GetEntityChangesEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/changes/{entityId:guid}", async (
            Guid entityId,
            IHandler dispatcher,
            CancellationToken cancellationToken,
            [FromQuery] SyncEntityType entityType) =>
        {
            var result = await dispatcher.QueryAsync<GetEntityChangesQuery, List<ChangeEntryRecord>>(new GetEntityChangesQuery(entityId, entityType), cancellationToken);
            return result.ToMinimalResult();
        })
        .RequireAuthorization()
        .WithTags("ChangesSync");
    }
}
