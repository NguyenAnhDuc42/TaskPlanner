using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class DeleteDocumentHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<DeleteDocumentHandler> logger
) : ICommandHandler<DeleteDocumentCommand, long>
{
    public async Task<Result<long>> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to delete document {DocumentId}", request.DocumentId);

        var document = await db.Documents
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.DeletedAt == null, cancellationToken);

        if (document == null)
        {
            logger.LogWarning("Document {DocumentId} not found or already deleted", request.DocumentId);
            return Result<long>.Failure(DocumentError.NotFound);
        }

        syncPermission.RequireCreatorOrAdmin(document.CreatorId ?? Guid.Empty);

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

            // Cascade the whole subtree (unbounded depth) + each descendant's DocumentBlocks.
            // Only ONE SyncEvent is emitted for the top-level delete — the client already knows
            // the full subtree locally and cascades it itself, same pattern as Space delete.
            var descendantIds = await DocumentCascadeHelper.GetDescendantIdsAsync(db, document.Id, cancellationToken);
            await DocumentCascadeHelper.CascadeDeleteAsync(db, descendantIds, cancellationToken);

            var syncPayload = JsonSerializer.Serialize(new { id = document.Id }, SyncJson.Options);

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = document.ProjectWorkspaceId,
                EntityType = SyncEntityType.Document,
                EntityId = document.Id,
                Action = SyncAction.D,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = memberId
            };

            db.SyncEvents.Add(syncEvent);
            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Deleted document {DocumentId}, cascaded {Count} descendant documents + their blocks", document.Id, descendantIds.Count);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(workspaceContext.WorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for deleted document {DocumentId}", document.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
