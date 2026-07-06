using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class UploadAttachmentEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/attachments/sync/upload", async (
            [FromForm] IFormFile file,
            IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, cancellationToken);

            var command = new UploadAttachmentCommand(ms.ToArray(), file.FileName, file.ContentType);
            var result = await dispatcher.SendAsync<UploadAttachmentCommand, UploadAttachmentResult>(command, cancellationToken);
            return result.ToMinimalResult();
        })
        .DisableAntiforgery()
        .RequireAuthorization()
        .WithTags("AttachmentsSync");
    }
}
