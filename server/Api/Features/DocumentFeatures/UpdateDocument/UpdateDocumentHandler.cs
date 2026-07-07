using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class UpdateDocumentHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<UpdateDocumentHandler> logger
) : ICommandHandler<UpdateDocumentCommand, long>
{
    public async Task<Result<long>> Handle(UpdateDocumentCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to update document {DocumentId}", request.DocumentId);

        var document = await db.Documents
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.DeletedAt == null, cancellationToken);

        if (document is null)
        {
            logger.LogWarning("Document {DocumentId} not found or deleted", request.DocumentId);
            return Result<long>.Failure(DocumentError.NotFound);
        }

        syncPermission.RequireMember();

        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        SyncEvent? syncEvent = null;

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var hasProcessed = await idempotencyService.HasProcessedAsync(request.TraceId, cancellationToken);
            if (hasProcessed)
            {
                logger.LogInformation("Idempotent bypass for trace {TraceId}. Skipping.", request.TraceId);
                return Result<long>.Success(0);
            }

            document.UpdateName(request.Name);

            var syncPayload = JsonSerializer.Serialize(new
            {
                id = document.Id,
                workspaceId = document.ProjectWorkspaceId,
                name = document.Name
            }, SyncJson.Options);

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = document.ProjectWorkspaceId,
                EntityType = SyncEntityType.Document,
                EntityId = document.Id,
                Action = SyncAction.U,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = memberId
            };

            db.SyncEvents.Add(syncEvent);

            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully updated document {DocumentId} in database with SyncEvent", document.Id);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(document.ProjectWorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for document {DocumentId}", document.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
