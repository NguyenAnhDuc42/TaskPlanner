# Feature Flow Patterns

> Reference for implementing features in TaskPlanner

---

## The 4 Patterns

| Pattern                  | When                 | Key Files                  |
| ------------------------ | -------------------- | -------------------------- |
| **Flow 1: Simple**       | Basic CRUD           | Handler only               |
| **Flow 2: + Cache**      | Frequently read data | `HybridCache`, `CacheKeys` |
| **Flow 3: + SignalR**    | Multi-user real-time | `IRealtimeService`         |
| **Flow 4: + Background** | Heavy async work     | `IBackgroundJobService`    |

---

## Flow 1: Simple CRUD

```
Request → Handler → Domain → UnitOfWork → Response
```

**Example:** `UpdateWorkspaceHandler`

```csharp
public async Task<Unit> Handle(UpdateCommand cmd, CancellationToken ct)
{
    var entity = await FindOrThrowAsync<Entity>(cmd.Id);
    await RequirePermissionAsync(entity, PermissionAction.Edit, ct);

    entity.Update(cmd.Name, cmd.Color);  // Domain method

    return Unit.Value;  // TransactionBehavior commits
}
```

---

## Flow 2: + Cache

**Read:** `Cache.GetOrCreateAsync()` → cache miss → DB → cache  
**Write:** DB change → `Cache.RemoveAsync()`

**Key files:**

- `Application/Common/CacheKeys.cs` - Key definitions
- Inject `HybridCache` in handlers that need it

**Example:** `GetMembersHandler`

```csharp
// READ: Get from cache or load
return await _cache.GetOrCreateAsync(
    CacheKeys.WorkspaceMembers(workspaceId),
    async ct => await LoadFromDb(workspaceId, ct),
    cancellationToken: ct);
```

**Example:** `AddMembersHandler`

```csharp
// WRITE: Do change, then invalidate
workspace.AddMembers(specs, CurrentUserId);
await _cache.RemoveAsync(CacheKeys.WorkspaceMembers(workspaceId), ct);
```

---

## Flow 3: + SignalR

**Purpose:** Notify OTHER connected clients about changes.

**Key files:**

- `Application/Interfaces/IRealtimeService.cs` - Interface
- `Infrastructure/Hubs/WorkspaceHub.cs` - SignalR hub
- `Infrastructure/Services/SignalRRealtimeService.cs` - Implementation

**Hub endpoint:** `/hubs/workspace`

**Client joins workspace:**

```javascript
connection.invoke("JoinWorkspace", workspaceId);
```

**Handler notifies:**

```csharp
// Fire-and-forget (don't await in request)
_ = _realtime.NotifyWorkspaceAsync(workspaceId, "MembersAdded", new
{
    AddedBy = CurrentUserId,
    MemberIds = newMemberIds
}, ct);
```

**Client listens:**

```javascript
connection.on("MembersAdded", (data) => {
  // Refresh members list
});
```

---

## Flow 4: + Background Jobs

**Purpose:** Heavy work that shouldn't block the request.

**Key files:**

- `Application/Interfaces/IBackgroundJobService.cs` - Interface
- `Background/Services/HangfireBackgroundJobService.cs` - Implementation
- `Background/Jobs/*` - Job classes

**Enqueue a job:**

```csharp
_jobService.Enqueue<MemberCleanupJob>(
    j => j.CleanupMemberDataAsync(workspaceId, userId));
```

**Job class:**

```csharp
public class MemberCleanupJob
{
    private readonly TaskPlanDbContext _context;

    public async Task CleanupMemberDataAsync(Guid workspaceId, Guid userId)
    {
        // Heavy cleanup logic - runs async
    }
}
```

---

## Combining Flows

A handler can use multiple patterns:

```csharp
public class RemoveMembersHandler : BaseCommandHandler
{
    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;
    private readonly IBackgroundJobService _jobs;

    public async Task<Unit> Handle(RemoveMembersCommand cmd, CancellationToken ct)
    {
        // 1. Do the work
        await SoftDeleteMembers(cmd.MemberIds, ct);

        // 2. Invalidate cache (Flow 2)
        await _cache.RemoveAsync(CacheKeys.WorkspaceMembers(cmd.WorkspaceId), ct);

        // 3. Notify viewers (Flow 3)
        _ = _realtime.NotifyWorkspaceAsync(cmd.WorkspaceId, "MembersRemoved", new { cmd.MemberIds }, ct);

        // 4. Enqueue heavy cleanup (Flow 4)
        foreach (var userId in cmd.MemberIds)
            _jobs.Enqueue<MemberCleanupJob>(j => j.CleanupMemberDataAsync(cmd.WorkspaceId, userId));

        return Unit.Value;
    }
}
```

---

## Quick Reference

| Need             | Use                                | Inject                  |
| ---------------- | ---------------------------------- | ----------------------- |
| Cache read       | `Cache.GetOrCreateAsync()`         | `HybridCache`           |
| Cache invalidate | `Cache.RemoveAsync()`              | `HybridCache`           |
| Notify clients   | `_realtime.NotifyWorkspaceAsync()` | `IRealtimeService`      |
| Async heavy work | `_jobs.Enqueue<T>()`               | `IBackgroundJobService` |
