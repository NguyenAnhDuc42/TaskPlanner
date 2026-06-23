using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Application;

public class UpdateSpaceStatusesHandler(
    TaskPlanDbContext db,
    WorkspaceContext context,
    HybridCache cache,
    RealtimeService realtime,
    PermissionService permissionService)
    : ICommandHandler<UpdateSpaceStatusesCommand>
{
    public async Task<Result> Handle(UpdateSpaceStatusesCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Admin, cancellationToken: cancellationToken);
        if (!hasAccess)
            return Result.Failure(MemberError.DontHavePermission);

        var existingStatuses = await db.Statuses
            .Where(s => s.ProjectSpaceId == request.SpaceId && s.DeletedAt == null)
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        foreach (var dto in request.Statuses)
        {
            switch (dto.Action)
            {
                case RowAction.Create:
                    var orderKey = ResolveOrderKey(dto.PreviousOrderKey, dto.NextOrderKey);
                    var newStatus = Status.Create(
                        context.WorkspaceId,
                        request.SpaceId,
                        dto.Name,
                        dto.Color,
                        dto.Category,
                        context.CurrentMember.Id,
                        orderKey,
                        dto.Id);
                    db.Statuses.Add(newStatus);
                    break;

                case RowAction.Update:
                    if (dto.Id is null || !existingStatuses.TryGetValue(dto.Id.Value, out var toUpdate)) continue;
                    toUpdate.Update(dto.Name, dto.Color, dto.Category, ResolveOrderKey(dto.PreviousOrderKey, dto.NextOrderKey));
                    break;

                case RowAction.Delete:
                    if (dto.Id is null || !existingStatuses.TryGetValue(dto.Id.Value, out var toDelete)) continue;
                    db.Statuses.Remove(toDelete);
                    break;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync($"Statuses-{context.WorkspaceId}", cancellationToken);

        await realtime.NotifyWorkspaceAsync(
            context.WorkspaceId,
            "StatusesUpdated",
            new { SpaceId = request.SpaceId },
            cancellationToken);

        return Result.Success();
    }

    private static string? ResolveOrderKey(string? previousKey, string? nextKey)
    {
        if (previousKey is null && nextKey is null) return null;
        if (previousKey is not null && nextKey is not null)
        {
            return string.Compare(previousKey, nextKey, StringComparison.Ordinal) >= 0
                ? FractionalIndex.After(previousKey)
                : FractionalIndex.Between(previousKey, nextKey);
        }
        if (previousKey is not null) return FractionalIndex.After(previousKey);
        return FractionalIndex.Before(nextKey!);
    }
}
