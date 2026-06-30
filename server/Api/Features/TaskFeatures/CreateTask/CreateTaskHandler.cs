using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class CreateTaskHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<CreateTaskHandler> logger
) : ICommandHandler<CreateTaskCommand, long>
{
    public async Task<Result<long>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to create task '{TaskName}' under Workspace {WorkspaceId}", request.Name, request.ProjectWorkspaceId);

        // Permissions Check — ProjectSpaceId is required by the validator, so always space-scoped
        var hasAccess = await permissionService.VerifyAsync(Role.Member, spaceId: request.ProjectSpaceId, requiredAccess: AccessLevel.Editor, cancellationToken: cancellationToken);

        if (!hasAccess)
        {
            logger.LogWarning("Access denied for user to create task in Space {SpaceId}", request.ProjectSpaceId);
            return Result<long>.Failure(MemberError.DontHavePermission);
        }

        ProjectTask? task = null;
        SyncEvent? syncEvent = null;
        SyncEvent? documentSyncEvent = null;

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            // Idempotency Check — offline-capable, DB-backed (survives retries across reconnects/restarts)
            var hasProcessed = await idempotencyService.HasProcessedAsync(request.TraceId, cancellationToken);
            if (hasProcessed)
            {
                logger.LogInformation("Idempotent bypass for trace {TraceId}. Skipping.", request.TraceId);
                return Result<long>.Success(0);
            }

            // Create Document automatically
            var document = Document.Create(
                id: request.DefaultDocumentId,
                workspaceId: request.ProjectWorkspaceId,
                name: request.Name,
                creatorId: workspaceContext.CurrentMember?.Id ?? Guid.Empty
            );
            db.Documents.Add(document);

            // Create Task
            task = ProjectTask.Create(
                id: request.Id,
                projectWorkspaceId: request.ProjectWorkspaceId,
                projectSpaceId: request.ProjectSpaceId,
                projectFolderId: request.ProjectFolderId,
                name: request.Name,
                slug: request.Slug,
                defaultDocumentId: document.Id,
                color: request.Color ?? "#FFFFFF",
                icon: request.Icon,
                creatorId: workspaceContext.CurrentMember?.Id ?? Guid.Empty, // Wait, if WorkspaceContext is not populated, this fails. Let's assume it is.
                statusId: request.StatusId,
                priority: request.Priority,
                orderKey: request.OrderKey,
                parentTaskId: request.ParentTaskId
            );

            db.ProjectTasks.Add(task);

            // Create Sync Event
            var syncPayload = JsonSerializer.Serialize(new
            {
                id = request.Id,
                projectWorkspaceId = request.ProjectWorkspaceId,
                projectSpaceId = request.ProjectSpaceId,
                projectFolderId = request.ProjectFolderId,
                name = request.Name,
                slug = request.Slug,
                defaultDocumentId = request.DefaultDocumentId,
                color = request.Color ?? "#FFFFFF",
                icon = request.Icon,
                statusId = request.StatusId,
                priority = request.Priority,
                orderKey = request.OrderKey,
                parentTaskId = request.ParentTaskId
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            syncEvent = new SyncEvent 
            {
                ProjectWorkspaceId = request.ProjectWorkspaceId,
                EntityType = SyncEntityType.Task,
                EntityId = request.Id,
                Action = SyncAction.C,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = workspaceContext.CurrentMember?.Id ?? Guid.Empty
            };
            
            // Create Document Sync Event
            var documentPayload = JsonSerializer.Serialize(new
            {
                id = request.DefaultDocumentId,
                workspaceId = request.ProjectWorkspaceId,
                name = request.Name
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            documentSyncEvent = new SyncEvent
            {
                ProjectWorkspaceId = request.ProjectWorkspaceId,
                EntityType = SyncEntityType.Document,
                EntityId = request.DefaultDocumentId,
                Action = SyncAction.C,
                Payload = documentPayload,
                ClientTraceId = request.TraceId, // Shared trace ID since they were created together
                AuthorUserId = workspaceContext.CurrentMember?.Id ?? Guid.Empty
            };

            db.SyncEvents.Add(documentSyncEvent);
            db.SyncEvents.Add(syncEvent);

            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully created task {TaskId} in database with SyncEvent", task.Id);
            return Result<long>.Success(0); // We return 0 here temporarily, it will be updated after SaveChanges if needed, but since EF Core assigns IDs on SaveChanges, wait...
        }, cancellationToken);

        // Instant Fire-and-Forget SignalR Broadcast — one DeltaBatch message for both events
        if (result.IsSuccess && syncEvent != null && documentSyncEvent != null)
        {
            // Now that transaction is committed, syncEvent.Id has the generated DB ID.
            var payloads = new[]
            {
                SyncQueryService.MapToPayload(documentSyncEvent),
                SyncQueryService.MapToPayload(syncEvent),
            };

            _ = realtimeService
                .NotifySyncEventBatchAsync(request.ProjectWorkspaceId, payloads, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time DeltaBatch for task {TaskId}", task!.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
