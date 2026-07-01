using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class UpdateSpaceHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<UpdateSpaceHandler> logger
) : ICommandHandler<UpdateSpaceCommand, long>
{
    public async Task<Result<long>> Handle(UpdateSpaceCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to update space {SpaceId}", request.SpaceId);

        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        var spaceData = await db.ProjectSpaces
            .Where(s => s.Id == request.SpaceId && s.DeletedAt == null)
            .Select(s => new {
                Space = s,
                CallerAccess = db.EntityAccesses
                    .Where(ea => ea.ProjectSpaceId == s.Id && ea.WorkspaceMemberId == memberId && ea.DeletedAt == null)
                    .Select(ea => (AccessLevel?)ea.AccessLevel).FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        var space = spaceData?.Space;
        if (space == null)
        {
            logger.LogWarning("Space {SpaceId} not found or deleted", request.SpaceId);
            return Result<long>.Failure(SpaceError.NotFound);
        }
        if (space.ProjectWorkspaceId != workspaceContext.WorkspaceId) return Result<long>.Failure(SpaceError.NotFound);

        if (!permissionService.Verify(Role.Member, space.IsPrivate, spaceData!.CallerAccess, AccessLevel.Manager, space.CreatorId))
        {
            logger.LogWarning("Access denied for user to update space {SpaceId}", space.Id);
            return Result<long>.Failure(MemberError.DontHavePermission);
        }

        SyncEvent? syncEvent = null;

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var hasProcessed = await idempotencyService.HasProcessedAsync(request.TraceId, cancellationToken);
            if (hasProcessed)
            {
                logger.LogInformation("Idempotent bypass for trace {TraceId}. Skipping.", request.TraceId);
                return Result<long>.Success(0);
            }

            var slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null;

            space.Update(
                name: request.Name,
                slug: slug,
                color: request.Color,
                icon: request.Icon,
                isPrivate: request.IsPrivate
            );

            var syncPayload = JsonSerializer.Serialize(new
            {
                id = space.Id,
                workspaceId = space.ProjectWorkspaceId,
                name = space.Name,
                slug = space.Slug,
                color = space.Color,
                icon = space.Icon,
                isPrivate = space.IsPrivate,
                orderKey = space.OrderKey,
                defaultDocumentId = space.DefaultDocumentId,
                isArchived = space.IsArchived
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = space.ProjectWorkspaceId,
                EntityType = SyncEntityType.Space,
                EntityId = space.Id,
                Action = SyncAction.U,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = workspaceContext.CurrentMember?.Id ?? Guid.Empty
            };

            db.SyncEvents.Add(syncEvent);
            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully updated space {SpaceId} in database with SyncEvent", space.Id);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(workspaceContext.WorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for space {SpaceId}", space.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
