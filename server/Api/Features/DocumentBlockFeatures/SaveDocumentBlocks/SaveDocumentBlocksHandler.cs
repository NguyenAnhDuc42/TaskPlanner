using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class SaveDocumentBlocksHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<SaveDocumentBlocksHandler> logger
) : ICommandHandler<SaveDocumentBlocksCommand, long>
{

    public async Task<Result<long>> Handle(SaveDocumentBlocksCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Saving {Count} block changes for document {DocumentId}", request.Blocks.Count, request.DocumentId);

        var workspaceId = await db.Documents.AsNoTracking()
            .Where(d => d.Id == request.DocumentId && d.DeletedAt == null)
            .Select(d => (Guid?)d.ProjectWorkspaceId)
            .FirstOrDefaultAsync(cancellationToken);

        if (workspaceId is null)
        {
            logger.LogWarning("Document {DocumentId} not found or deleted", request.DocumentId);
            return Result<long>.Failure(DocumentError.NotFound);
        }

        syncPermission.RequireMember();

        if (request.Blocks.Count == 0)
            return Result<long>.Success(0);

        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var hasProcessed = await idempotencyService.HasProcessedAsync(request.TraceId, cancellationToken);
            if (hasProcessed)
            {
                logger.LogInformation("Idempotent bypass for trace {TraceId}. Skipping.", request.TraceId);
                return Result<long>.Success(0);
            }

            // Load all existing blocks in one query
            var existingIds = request.Blocks.Where(b => !b.IsDeleted).Select(b => b.Id).ToHashSet();
            var deleteIds = request.Blocks.Where(b => b.IsDeleted).Select(b => b.Id).ToHashSet();
            var allIds = existingIds.Union(deleteIds).ToHashSet();

            var existingBlocks = allIds.Count > 0
                ? await db.DocumentBlocks
                    .Where(b => allIds.Contains(b.Id) && b.DeletedAt == null)
                    .ToDictionaryAsync(b => b.Id, cancellationToken)
                : [];

            foreach (var item in request.Blocks)
            {
                if (item.IsDeleted)
                {
                    if (!existingBlocks.TryGetValue(item.Id, out var block)) continue;
                    block.SoftDelete();
                    events.Add(new SyncEvent {
                        ProjectWorkspaceId = workspaceId.Value, EntityType = SyncEntityType.DocumentBlock,
                        EntityId = block.Id, Action = SyncAction.D, ClientTraceId = request.TraceId, AuthorUserId = memberId,
                        Payload = JsonSerializer.Serialize(new { id = block.Id }, SyncJson.Options)
                    });
                }
                else if (existingBlocks.TryGetValue(item.Id, out var block))
                {
                    // Update
                    block.UpdateContent(item.Content);
                    block.UpdateOrderKey(item.OrderKey);
                    block.UpdateType(item.Type);
                    events.Add(new SyncEvent {
                        ProjectWorkspaceId = workspaceId.Value, EntityType = SyncEntityType.DocumentBlock,
                        EntityId = block.Id, Action = SyncAction.U, ClientTraceId = request.TraceId, AuthorUserId = memberId,
                        Payload = JsonSerializer.Serialize(new {
                            id = block.Id, documentId = request.DocumentId, workspaceId = workspaceId.Value,
                            type = block.Type, content = block.Content, orderKey = block.OrderKey
                        }, SyncJson.Options)
                    });
                }
                else
                {
                    // Create
                    var newBlock = DocumentBlock.CreateWithId(item.Id, workspaceId.Value, request.DocumentId, item.Type, item.Content, item.OrderKey, memberId);
                    db.DocumentBlocks.Add(newBlock);
                    events.Add(new SyncEvent {
                        ProjectWorkspaceId = workspaceId.Value, EntityType = SyncEntityType.DocumentBlock,
                        EntityId = newBlock.Id, Action = SyncAction.C, ClientTraceId = request.TraceId, AuthorUserId = memberId,
                        Payload = JsonSerializer.Serialize(new {
                            id = newBlock.Id, documentId = request.DocumentId, workspaceId = workspaceId.Value,
                            type = newBlock.Type, content = newBlock.Content, orderKey = newBlock.OrderKey
                        }, SyncJson.Options)
                    });
                }
            }

            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Saved {Count} block changes for document {DocumentId}", events.Count, request.DocumentId);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && events.Count > 0)
        {
            var payloads = events.Select(SyncQueryService.MapToPayload).ToArray();
            _ = realtimeService
                .NotifySyncEventBatchAsync(workspaceId.Value, payloads, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to broadcast block changes for document {DocumentId}", request.DocumentId),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(events[^1].Id);
        }

        return result.IsSuccess
            ? Result<long>.Success(0)
            : Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
