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

        var document = await db.Documents.AsNoTracking()
            .Where(d => d.Id == request.DocumentId && d.DeletedAt == null)
            .Select(d => new { d.ProjectWorkspaceId })
            .FirstOrDefaultAsync(cancellationToken);

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

            var block = DocumentBlock.CreateWithId(request.Id, document.ProjectWorkspaceId, request.DocumentId, request.Type, request.Content ?? string.Empty, request.OrderKey, memberId);
            db.DocumentBlocks.Add(block);

            var syncPayload = JsonSerializer.Serialize(new
            {
                id = block.Id,
                documentId = block.DocumentId,
                workspaceId = block.ProjectWorkspaceId,
                type = block.Type,
                content = block.Content,
                orderKey = block.OrderKey
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = document.ProjectWorkspaceId,
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
                .NotifySyncEventAsync(document.ProjectWorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for document block {BlockId}", request.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
