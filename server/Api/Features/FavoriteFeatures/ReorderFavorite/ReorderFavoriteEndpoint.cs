using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class ReorderFavoriteEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/favorites/reorder", async (
            [FromBody] ReorderFavoriteCommand request,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok()
                : Results.BadRequest(result.Error);
        })
        .RequireAuthorization()
        .WithTags("FavoritesSync");
    }
}
