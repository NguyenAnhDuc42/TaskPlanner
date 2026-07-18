using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class CreateDocumentHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<CreateDocumentHandler> logger
) : ICommandHandler<CreateDocumentCommand, long>
{
    public async Task<Result<long>> Handle(CreateDocumentCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to create document '{DocumentName}' in space {SpaceId}", request.Name, request.SpaceId);

        var space = await db.ProjectSpaces
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SpaceId && s.DeletedAt == null, cancellationToken);
        if (space is null)
        {
            logger.LogWarning("Space {SpaceId} not found or deleted", request.SpaceId);
            return Result<long>.Failure(SpaceError.NotFound);
        }

        // Defense against cross-space reparenting — a Document's parent must live in the same Space.
        if (request.ParentDocumentId.HasValue)
        {
            var parentSpaceId = await db.Documents
                .AsNoTracking()
                .Where(d => d.Id == request.ParentDocumentId.Value && d.DeletedAt == null)
                .Select(d => (Guid?)d.ProjectSpaceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (parentSpaceId is null || parentSpaceId != request.SpaceId)
            {
                logger.LogWarning("Parent document {ParentDocumentId} not found in space {SpaceId}", request.ParentDocumentId, request.SpaceId);
                return Result<long>.Failure(DocumentError.NotFound);
            }
        }

        syncPermission.RequireMember();

        var creatorId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        Document? document = null;
        SyncEvent? syncEvent = null;

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var hasProcessed = await idempotencyService.HasProcessedAsync(request.TraceId, cancellationToken);
            if (hasProcessed)
            {
                logger.LogInformation("Idempotent bypass for trace {TraceId}. Skipping.", request.TraceId);
                return Result<long>.Success(0);
            }

            var maxKey = await db.Documents
                .AsNoTracking()
                .Where(d => d.ProjectSpaceId == request.SpaceId && d.ParentDocumentId == request.ParentDocumentId && d.DeletedAt == null)
                .Select(d => (string?)d.OrderKey)
                .OrderByDescending(k => k)
                .FirstOrDefaultAsync(cancellationToken);

            var orderKey = FractionalIndex.SafeAfter(maxKey);

            document = Document.Create(
                id: request.Id,
                projectWorkspaceId: workspaceContext.WorkspaceId,
                projectSpaceId: request.SpaceId,
                name: request.Name,
                orderKey: orderKey,
                creatorId: creatorId,
                parentDocumentId: request.ParentDocumentId,
                icon: request.Icon,
                color: request.Color
            );
            db.Documents.Add(document);

            var syncPayload = JsonSerializer.Serialize(new
            {
                id = document.Id,
                workspaceId = workspaceContext.WorkspaceId,
                spaceId = document.ProjectSpaceId,
                parentDocumentId = document.ParentDocumentId,
                name = document.Name,
                orderKey = document.OrderKey,
                icon = document.Icon,
                color = document.Color,
                createdAt = document.CreatedAt
            }, SyncJson.Options);

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = workspaceContext.WorkspaceId,
                EntityType = SyncEntityType.Document,
                EntityId = document.Id,
                Action = SyncAction.C,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = creatorId
            };

            db.SyncEvents.Add(syncEvent);
            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully created document {DocumentId} in database with SyncEvent", document.Id);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(workspaceContext.WorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for document {DocumentId}", document!.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
