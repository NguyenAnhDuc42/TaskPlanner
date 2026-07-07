using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class DeleteDocumentBlockHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<DeleteDocumentBlockHandler> logger
) : ICommandHandler<DeleteDocumentBlockCommand, long>
{
    public async Task<Result<long>> Handle(DeleteDocumentBlockCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to delete document block {BlockId}", request.BlockId);

        var block = await db.DocumentBlocks
            .FirstOrDefaultAsync(b => b.Id == request.BlockId && b.DeletedAt == null, cancellationToken);

        if (block is null)
        {
            logger.LogWarning("Document block {BlockId} not found or already deleted", request.BlockId);
            return Result<long>.Failure(DocumentBlockError.NotFound);
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

            block.SoftDelete();

            var syncPayload = JsonSerializer.Serialize(new { id = block.Id },
                SyncJson.Options);

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = block.ProjectWorkspaceId,
                EntityType = SyncEntityType.DocumentBlock,
                EntityId = block.Id,
                Action = SyncAction.D,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = memberId
            };

            db.SyncEvents.Add(syncEvent);

            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully deleted document block {BlockId} in database with SyncEvent", block.Id);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(block.ProjectWorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for deleted document block {BlockId}", block.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
