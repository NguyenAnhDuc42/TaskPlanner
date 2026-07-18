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

        if (document == null)
        {
            logger.LogWarning("Document {DocumentId} not found or deleted", request.DocumentId);
            return Result<long>.Failure(DocumentError.NotFound);
        }

        syncPermission.RequireCreatorOrAdmin(document.CreatorId ?? Guid.Empty);

        // Server-side cycle guard: walk up from the requested new parent to the root. If we hit
        // the document being moved, this move would create a cycle in the tree — reject it. This
        // is defense-in-depth alongside the client-side guard (Phase 3's DnD cycle check).
        if (!request.ClearParent && request.ParentDocumentId.HasValue)
        {
            var currentId = request.ParentDocumentId.Value;
            var guard = 0;
            while (currentId != Guid.Empty && guard++ < 1000)
            {
                if (currentId == document.Id)
                {
                    logger.LogWarning("Rejected move of document {DocumentId} under its own descendant {TargetParentId}", document.Id, request.ParentDocumentId);
                    return Result<long>.Failure(DocumentError.CircularReference);
                }

                var parent = await db.Documents
                    .AsNoTracking()
                    .Where(d => d.Id == currentId && d.DeletedAt == null)
                    .Select(d => new { d.ParentDocumentId, d.ProjectSpaceId })
                    .FirstOrDefaultAsync(cancellationToken);

                if (parent is null) break;
                if (parent.ProjectSpaceId != document.ProjectSpaceId)
                {
                    logger.LogWarning("Rejected move of document {DocumentId} to a parent in a different space", document.Id);
                    return Result<long>.Failure(DocumentError.NotFound);
                }

                currentId = parent.ParentDocumentId ?? Guid.Empty;
            }
        }

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

            document.Update(
                name: request.Name,
                parentDocumentId: request.ParentDocumentId,
                clearParent: request.ClearParent,
                orderKey: request.OrderKey,
                icon: request.Icon,
                color: request.Color
            );

            var syncPayload = JsonSerializer.Serialize(new
            {
                id = document.Id,
                workspaceId = document.ProjectWorkspaceId,
                spaceId = document.ProjectSpaceId,
                parentDocumentId = document.ParentDocumentId,
                name = document.Name,
                orderKey = document.OrderKey,
                icon = document.Icon,
                color = document.Color
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

            logger.LogInformation("Successfully updated document {DocumentId}", document.Id);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(workspaceContext.WorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for document {DocumentId}", document.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
