using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class CreateDocumentBlockHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<CreateDocumentBlockHandler> logger
) : ICommandHandler<CreateDocumentBlockCommand, long>
{
    public async Task<Result<long>> Handle(CreateDocumentBlockCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to create block on Document {DocumentId}", request.DocumentId);

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

            var block = DocumentBlock.CreateWithId(request.Id, workspaceContext.WorkspaceId, request.DocumentId, request.Type, request.Content ?? string.Empty, request.OrderKey, memberId);
            db.DocumentBlocks.Add(block);

            var syncPayload = JsonSerializer.Serialize(new
            {
                id = block.Id,
                documentId = block.DocumentId,
                workspaceId = block.ProjectWorkspaceId,
                type = block.Type,
                content = block.Content,
                orderKey = block.OrderKey
            }, SyncJson.Options);

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = workspaceContext.WorkspaceId,
                EntityType = SyncEntityType.DocumentBlock,
                EntityId = block.Id,
                Action = SyncAction.C,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = memberId
            };

            db.SyncEvents.Add(syncEvent);

            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully created document block {BlockId} in database with SyncEvent", block.Id);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(workspaceContext.WorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for document block {BlockId}", request.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
