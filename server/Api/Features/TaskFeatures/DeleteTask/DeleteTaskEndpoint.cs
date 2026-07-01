using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class DeleteTaskEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/tasks/sync/{id:guid}", async (
            Guid id,
            [FromHeader(Name = "X-Client-Trace-Id")] string? traceId,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return Results.BadRequest(new { Error = "X-Client-Trace-Id header is required for offline sync." });
            }

            var request = new DeleteTaskCommand { TaskId = id, TraceId = traceId };

            var result = await dispatcher.SendAsync<DeleteTaskCommand, long>(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { SyncEventId = result.Value })
                : Results.BadRequest(result.Error);
        })
        .RequireAuthorization()
        .WithTags("TasksSync");
    }
}
