# Workspace Architecture Guidelines

## Command Flow

Each command follows the structure:
1. **Command**
2. **Validator**
3. **Handler**

### Implementation Details:
- **EF Core**: Commands use EF Core (`TaskPlanDbContext`) for data modifications. 
- **Global Query Filters**: Because the DbContext uses global query filters, **you do not need to explicitly filter by `WorkspaceId` in your EF Core lookups**.
  - **Preferred:** `await dbContext.ProjectTasks.FirstOrDefaultAsync(x => x.Id == request.TaskId, ct);`
  - **Avoid:** `FindAsync(...)` and `SingleAsync(...)` for tenant-scoped lookups as they can bypass intended tenant filtering patterns or fail unexpectedly.
- **SignalR Responses**: Responses are sent asynchronously through SignalR, returning only a `Result.Success()`.
- **Batch Models**: Command responses broadcasted via SignalR should use the shared batch response models: `EntityBatchUpdate` and `EntityBatchDelete`.
  - **Updates** return collections of modified entities grouped by type.
  - **Deletes** return collections of deleted entity IDs grouped by type.

**Example Update Response:**
```csharp
var updatePayload = new EntityBatchUpdate
{
    Tasks = [taskRecord]
};
await realtimeService.NotifyEntitiesUpdatedAsync(workspaceId, updatePayload, ct);
```

**Example Delete Response:**
```csharp
var deletePayload = new EntityBatchDelete
{
    TaskIds = [taskId]
};
await realtimeService.NotifyEntitiesDeletedAsync(workspaceId, deletePayload, ct);
```

This response pattern is intended to be the standard across the entire Workspace bounded context. The frontend maintains a normalized `FlatItem` store and applies optimistic updates using these payloads.

**Current supported entities:**
Spaces, Folders, Tasks, Members, Assignees, Entity Access. *(Additional entity types should be added to the batch models as the system grows).*

---

## Query Flow

Each query follows the structure:
1. **Query**
2. **Handler**

### Implementation Details:
- **Dapper**: Queries use Dapper for high-performance reads.
- **Tenant Isolation**: **Unlike EF Core**, raw Dapper SQL queries bypass global query filters. **Every entity lookup in Dapper must explicitly include workspace constraints** in the `WHERE` clause to guarantee proper isolation.
  - **Example**: `WHERE Id = @Id AND ProjectWorkspaceId = @WorkspaceId`
- **Flat Data Philosophy**: Query responses should follow the same flat data philosophy used in commands. Avoid nested DTO hierarchies. Prefer multiple record collections that can be normalized directly by the frontend.

**Example Response Payload:**
```csharp
public sealed record GetWorkspaceResponse(
    List<SpaceRecord> Spaces,
    List<FolderRecord> Folders,
    List<TaskRecord> Tasks,
    List<MemberRecord> Members
);
```
The backend should provide normalized data and allow the frontend to construct tree structures and visual hierarchies.

---

## Caching Strategy

Even though the frontend utilizes Redux as a client-side store, we implement **Backend Query Caching**.

**Why cache when we save items in the Redux store?**
If a user hard-refreshes the page by accident (or opens a new tab), the Redux state is lost and requires a full initial query. Caching at the query/handler layer allows the backend to return that data instantly, improving perceived performance on cold loads. 
- *Write-Through Caching*: When saving data via Commands, updates should invalidate or update the cache simultaneously so subsequent queries hit warm, accurate data.

---

## Permission System

The `PermissionService` is the standard mechanism for authorization checks.
- Handlers should **not** contain custom permission logic when it can be delegated to the Permission Service.
- The service checks the `Role` (found inside `WorkspaceMember`) at the Workspace level. 
- If checking a Space, it evaluates the `AccessLevel` (inside `EntityAccess`).
- Space permissions naturally cascade downward to Folders and Tasks.

---

## Frontend Responsibilities
- Entity normalization
- Relationship mapping
- Hierarchy construction
- State management
- Optimistic updates
