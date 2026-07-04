using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Application;

public class SyncQueryService(TaskPlanDbContext db)
{
    public const int CurrentDatabaseVersion = 3;

    public async Task<SyncDeltaBatch> GetChangesAsync(Guid workspaceId, long since, CancellationToken cancellationToken = default)
    {
        var events = await db.SyncEvents
            .Where(e => e.ProjectWorkspaceId == workspaceId && e.Id > since)
            .OrderBy(e => e.Id)
            .ToListAsync(cancellationToken);

        var latestPerEntity = events
            .GroupBy(e => (e.EntityType, e.EntityId))
            .Select(g => g.OrderBy(e => e.Id).Last())
            .OrderBy(e => e.Id)
            .ToList();

        var actions = latestPerEntity.Select(MapToPayload).ToList();
        var latestSyncId = events.Count > 0 ? events[^1].Id : since;

        return new SyncDeltaBatch(actions, CurrentDatabaseVersion, latestSyncId);
    }

    public async Task<long> GetLastSyncIdAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var hasAny = await db.SyncEvents.AnyAsync(e => e.ProjectWorkspaceId == workspaceId, cancellationToken);
        if (!hasAny) return 0;

        return await db.SyncEvents
            .Where(e => e.ProjectWorkspaceId == workspaceId)
            .MaxAsync(e => e.Id, cancellationToken);
    }

    public static SyncEventPayload MapToPayload(SyncEvent e) => new(
        SyncId: e.Id,
        Action: e.Action.ToString(),
        EntityType: e.EntityType.ToString(),
        EntityId: e.EntityId,
        Data: JsonDocument.Parse(e.Payload).RootElement,
        ClientTraceId: e.ClientTraceId
    );
}
