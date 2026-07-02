using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class BatchFlushEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/sync/batch", async (
            [FromBody] BatchFlushCommand command,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync<BatchFlushCommand, BatchFlushResult>(command, cancellationToken);
            return result.ToMinimalResult();
        })
        .RequireAuthorization()
        .WithTags("Sync");
    }
}
