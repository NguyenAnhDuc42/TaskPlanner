using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Application;

public class SyncQueryService(TaskPlanDbContext db)
{
    // Bump whenever Bootstrap's payload shape changes (new entity type added, field removed,
    // etc.) — clients compare this against the version stored from their last bootstrap and
    // force a fresh one if it's stale, instead of only bootstrapping on a truly first-ever
    // session. See sync-engine.ts's init(). v2: added Members to Bootstrap.
    public const int CurrentDatabaseVersion = 2;

    public async Task<SyncDeltaBatch> GetChangesAsync(Guid workspaceId, long since, CancellationToken cancellationToken = default)
    {
        var events = await db.SyncEvents
            .Where(e => e.ProjectWorkspaceId == workspaceId && e.Id > since)
            .OrderBy(e => e.Id)
            .ToListAsync(cancellationToken);

        // Collapse to the latest event per (EntityType, EntityId) — a catching-up client only
        // needs to reach the current end state, not replay every intermediate change. Safe
        // because every SyncEvent payload is always a full entity snapshot (never a partial
        // diff) and the client's applyDelta treats C/U identically (both just upsert). If an
        // entity's last event in this window is a Delete, sending just that Delete is safe even
        // if the client never knew the entity existed — removing a record it never had is a
        // no-op. latestSyncId still comes from the full (uncollapsed) list so the client's
        // checkpoint doesn't skip past events that got collapsed away.
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
