# Backend Sync Architecture & Context

*Use this document as context for future chats to quickly spin up AI assistants on the current state of the backend sync system.*

**Companion docs:** `FRONTEND_SYNC_CONTEXT.md` (repo root) covers the client side of this same system. `SYNC_SCENARIOS.md` (repo root) is the living checklist of what's actually built/tested vs not — check it before assuming a scenario works.

## 1. Where new code lives — `Api` project, not `Application`
The existing app is vertical-slice architecture inside the `Application` project (legacy, untouched, still serves the old REST/Redux/RTK-Query frontend on the old `feature/sync-engine` branch's predecessor). **All new sync-system code goes in the `Api` project** as minimal-API slices, deliberately separate from the old `Application`-layer Controllers.

```
Api/Features/
  TaskFeatures/CreateTask/        — CreateTaskCommand, CreateTaskHandler, CreateTaskValidator, CreateTaskEndpoint
  TaskFeatures/UpdateTask/        — UpdateTaskCommand, UpdateTaskHandler, UpdateTaskValidator, UpdateTaskEndpoint (PUT /api/tasks/sync/{id})
  TaskFeatures/DeleteTask/        — DeleteTaskCommand, DeleteTaskHandler, DeleteTaskValidator, DeleteTaskEndpoint (DELETE /api/tasks/sync/{id})
  SpaceFeatures/CreateSpace/      — CreateSpaceCommand, CreateSpaceHandler, CreateSpaceValidator, CreateSpaceEndpoint (POST /api/spaces/sync)
  SpaceFeatures/UpdateSpace/      — UpdateSpaceCommand, UpdateSpaceHandler, UpdateSpaceValidator, UpdateSpaceEndpoint (PUT /api/spaces/sync/{id})
  SpaceFeatures/DeleteSpace/      — DeleteSpaceCommand, DeleteSpaceHandler, DeleteSpaceValidator, DeleteSpaceEndpoint (DELETE /api/spaces/sync/{id})
  FolderFeatures/CreateFolder/    — CreateFolderCommand, CreateFolderHandler, CreateFolderValidator, CreateFolderEndpoint (POST /api/folders/sync)
  FolderFeatures/UpdateFolder/    — UpdateFolderCommand, UpdateFolderHandler, UpdateFolderValidator, UpdateFolderEndpoint (PUT /api/folders/sync/{id})
  FolderFeatures/DeleteFolder/    — DeleteFolderCommand, DeleteFolderHandler, DeleteFolderValidator, DeleteFolderEndpoint (DELETE /api/folders/sync/{id})
  CommentFeatures/Create|Update|DeleteComment/  — CRUD at /api/comments/sync (POST, PUT/DELETE {id}); Create requires Viewer, Update is creator-only, Delete is creator-bypass-or-Editor; soft-delete (diverges from legacy hard-delete, deliberate — see below)
  DocumentFeatures/Update|DeleteDocument/        — Update/Delete at /api/documents/sync/{id}; no standalone Create (Document is only ever created as a Task/Space side-effect); scoped via DocumentScopeResolver
  DocumentBlockFeatures/Create|Update|DeleteDocumentBlock/ — single-entity CRUD at /api/document-blocks/sync (replaces legacy bulk PUT /documents/{id}/blocks for the sync path — one SyncEvent per block, matches TransactionQueue's one-transaction-per-mutation model); scoped via DocumentScopeResolver
  SyncFeatures/Bootstrap/         — GetBootstrapQuery, GetBootstrapHandler, GetBootstrapEndpoint
  SyncFeatures/GetChanges/        — GetChangesQuery, GetChangesHandler, GetChangesEndpoint
  SyncFeatures/BatchFlush/        — BatchFlushCommand, BatchFlushHandler, BatchFlushEndpoint (POST /api/sync/batch) ⬜ not yet built
Api/Extensions/
  EndpointMappingExtensions.cs    — reflection-based MapEndpoint() auto-registration (see §3)
  MinimalResultExtensions.cs      — Result→IResult conversion for minimal APIs (see §4 — IMPORTANT gotcha)
  ResultExtensions.cs              — OLD Result→IActionResult conversion, only for the legacy Controllers/*.cs

Application/
  Hubs/SyncHub.cs                  — new SignalR hub at /hubs/sync (separate from old /hubs/workspace)
  Services/Sync/SyncQueryService.cs — shared delta/bootstrap query logic (used by both REST endpoints AND SyncHub)
  Services/Sync/SyncEventPayload.cs — wire DTO: SyncEventPayload, SyncDeltaBatch
  Services/Sync/IdempotencyService.cs — DB-backed (ProcessedTraces table), offline-capable idempotency check
  Services/RealtimeService.cs       — added NotifySyncEventAsync (single) + NotifySyncEventBatchAsync (batched)
  Data/Configurations/SyncEventConfiguration.cs, ProcessedTraceConfiguration.cs
  Data/Migrations/20260630083003_AddSyncEventsAndProcessedTraces.cs

Domain/
  Entities/Sync/SyncEvent.cs, ProcessedTrace.cs
  Enums/Sync/SyncAction.cs (C/U/D), SyncEntityType.cs (Space/Folder/Task/Status/Comment/Document/DocumentBlock/Member/EntityAccess)
```

## 2. Routing convention — `/sync` suffix
New minimal-API mutation endpoints live at `/api/{entity}s/sync` (e.g. `POST /api/tasks/sync`), **not** the old `/api/tasks` REST route — that old route still exists (`TasksController`, old `Application.CreateTaskCommand`) serving the legacy frontend. The two coexist on different paths during migration. Query endpoints (bootstrap/changes) live at `/api/workspaces/{id}/sync/bootstrap` and `/api/workspaces/{id}/sync/changes?since={syncId}`.

`UpdateTask`/`DeleteTask` now exist at `PUT /api/tasks/sync/{id}` and `DELETE /api/tasks/sync/{id}` — matches what the frontend's `SyncEngine.getRequestConfig()` already assumed.

**Naming collision gotcha (hit twice now):** the old `TasksController` (legacy MVC) takes `Application.UpdateTaskCommand`/`Application.DeleteTaskCommand` by unqualified name. When a new `Api.UpdateTaskCommand`/`Api.DeleteTaskCommand` is added in the same root namespace area, `TasksController.cs` stops compiling (ambiguous reference) — fully-qualify the legacy ones (`Application.UpdateTaskCommand`, `new Application.DeleteTaskCommand(id)`) in `TasksController.cs` whenever a new `Api`-namespaced command of the same name is introduced.

**Update/Delete payload notes:**
- `UpdateTaskHandler` loads the existing task, regenerates `slug` from `name` if `name` changed (mirrors legacy `UpdateTaskHandler`), applies `ProjectTask.Update(...)`, writes one `SyncEvent` (Action `U`) with the *post-update* entity snapshot as payload (not just the changed fields — the client's `applyDelta` does a full `dbPut`/`upsert`, so partial payloads would silently drop unmentioned fields client-side... actually re-verify this against `delta-handler.ts`'s `dbPut`, which calls `taskDB.put(data)` — IndexedDB `put` fully overwrites the record, so the delta payload must always be the *complete* entity, never a partial diff).
- `DeleteTaskHandler` calls `task.SoftDelete()` (sets `DeletedAt`, **not** `Archive()`/`IsArchived`) — deliberately matches the legacy delete semantics and the frontend delta-handler's `'D'` action, which fully removes the entity from `TaskStore` + IndexedDB on apply. The frontend mutation's optimistic `isArchived: true` patch is just a same-tick placeholder before the real `Delta` arrives and removes it outright — don't be misled by that into making the backend set `IsArchived` instead of soft-deleting.
- `ClearStartDate`/`ClearDueDate` exist on `UpdateTaskCommand` (mirroring the legacy command) but the frontend's `TaskMutations.update()` never sends them — date-clearing via the sync path is unimplemented client-side, tracked in `SYNC_SCENARIOS.md`.

**Space slices — a bigger fan-out than Task.** `CreateSpace` doesn't just create one entity: mirroring the legacy `CreateSpaceHandler`, it also auto-creates a default `Document`, a 4-status starter set (`Status.CreateSpaceStarterSet` — Planned/In Progress/Paused/Completed), and an `EntityAccess` row granting the creator `Manager` access. That's **7 `SyncEvent` rows in one create** (1 Document + 1 Space + 4 Status + 1 EntityAccess), all sharing the same `ClientTraceId`, broadcast as a single `NotifySyncEventBatchAsync` call. Only `Document`/`Space` IDs are client-dictated (added an explicit-id `ProjectSpace.Create(id, ...)` overload, same pattern as `ProjectTask`/`Document`); the 4 Statuses and the EntityAccess row keep server-generated IDs since nothing needs to optimistically reference them before the space exists.

`UpdateSpace` is single-entity, same shape as `UpdateTask`. `DeleteSpace` always requires `Role.Admin` (no "creator can delete their own" bypass like Task has) — mirrors the legacy `DeleteSpaceHandler` exactly.

**`DeleteSpace` — tombstone approach (cascade via client, not server events).** The backend does NOT emit individual D events for each child folder/task. Instead:
1. `ExecuteUpdateAsync` bulk soft-deletes all tasks in the space (`deleted_at`, `updated_at` set atomically)
2. `ExecuteUpdateAsync` bulk soft-deletes all folders in the space
3. `ExecuteUpdateAsync` bulk soft-deletes all **statuses** in the space (critical — missing this caused stale statuses to appear in bootstrap; `statuses.deleted_at IS NULL` was erroneously true for statuses from deleted spaces)
4. `space.Delete()` soft-deletes the space itself
5. A **single Space D SyncEvent** is emitted and broadcast via `NotifySyncEventAsync`

The client that receives Space D is responsible for cascading removal of all children locally (see `FRONTEND_SYNC_CONTEXT.md §9` — delta-handler Space D case). This is deliberate: emitting one event per child entity would create O(n) SignalR messages and require the client to process them individually, when a single Space D with client-side cascade is equivalent and far cheaper.

**`DeleteFolder` — reparent tasks, then tombstone.** Backend emits N Task U events (one per orphaned task, with `folderId: null`) + one Folder D event, broadcast as a single `NotifySyncEventBatchAsync` call. The client initiating the delete reparents tasks locally immediately; other clients receive the Task U deltas and reparent via the normal upsert path.

**Folder slices** follow the same naming-collision gotcha as Space — had to fully-qualify `Application.CreateFolderCommand` etc. in the legacy `FolderController.cs` when the new `Api`-namespaced commands were added.

**Same naming-collision gotcha as Task** — had to fully-qualify `Application.CreateSpaceCommand`/`UpdateSpaceCommand`/`DeleteSpaceCommand` in the legacy `SpacesController.cs` (`server/Api/Controllers/SpaceController.cs`) once the new `Api`-namespaced commands of the same name were added. This is now the third time this exact issue has been hit (Task, then Task again for Update/Delete, now Space) — expect it every time a new entity gets sync slices; check the corresponding legacy `*Controller.cs` first.

**Frontend note (corrected — the line below was stale):** `delta-handler.ts`'s `getEntityApplier()` actually already handles `Workspace`, `Space`, `Folder`, `Task`, `Document`, `Status`, and `EntityAccess` — only `Comment` and `DocumentBlock` still hit the `default` branch and get dropped with a `console.warn`. Don't trust this doc's age on frontend claims — check `delta-handler.ts` directly, it's moved faster than this doc was updated.

**Workspace/Status/Member/EntityAccess pass (added after Comment/Document/DocumentBlock).** Ported the remaining entities per the local-first-vs-backend-first split Duc specified:
- **Workspace**: `CreateWorkspace`/`UpdateWorkspace` already existed under `Api/Features/WorkspaceFeatures` but were legacy-pattern-in-new-clothing — no `SyncEvent`, no idempotency, no broadcast. **This was a live bug**: the frontend's `WorkspaceMutations.update()` (`frontend/src/mutations/workspace.mutations.ts`) enqueues into `TransactionQueue` and waits for a `Delta` to dequeue the transaction — since `UpdateWorkspaceHandler` never emitted one, every workspace rename got stuck in the queue forever, silently. Fixed: both now follow the full pattern (idempotency + transaction + `SyncEvent` + `NotifySyncEventAsync`). Also built `DeleteWorkspaceCommand` from scratch — no reachable delete-workspace endpoint existed anywhere (`Application.DeleteWorkspaceCommand` existed but no controller route ever called it), yet the frontend's `delete()` was already calling `DELETE /api/workspaces/{id}` (no `/sync` suffix, matching its backend-first/no-queue design) — that call was a live 404 until now.
- **Status**: `UpdateSpaceStatusesCommand` ported to `Api/Features/StatusFeatures/UpdateSpaceStatuses` as a batch endpoint (`PUT /api/statuses/sync/batch`) — one call carries a list of `StatusUpdateValue` rows each tagged with `RowAction` (Create/Update/Delete), emits one `SyncEvent` per row, broadcasts as a single `NotifySyncEventBatchAsync`. Delete rows now soft-delete (legacy hard-deleted via `db.Statuses.Remove`).
- **Member**: only `UpdateMembers` (role/status) and `RemoveMembers` ported — no `Create` slice, since member creation stays email-invite-based on the legacy `POST /workspaces/{id}/members` route by design. Same batch-row shape as Status/EntityAccess for update; remove takes a flat `List<Guid>`. Routes: `PUT /api/members/sync/batch`, `POST /api/members/sync/remove` (POST not DELETE — a body-bearing batch delete is unreliable over DELETE across HTTP clients).
- **EntityAccess**: `EntityAccessBatchCommand` ported to `Api/Features/EntityAccessFeatures/EntityAccessBatch` (`PUT /api/entity-access/sync/batch`), same Create/Update/Delete-row-with-RowAction shape. Delete rows now soft-delete via `entity.Remove()` (which already called `SoftDelete()` — the legacy handler was bypassing its own domain method and hard-deleting via `db.EntityAccesses.Remove` instead).
- **Naming collision gotcha hit again** (5th+ time) for all four: `SpaceController.cs` (`UpdateStatuses`, `UpdateAccess`) and `WorkspaceController.cs` (`UpdateMembers`, `RemoveMembers`) all needed their legacy command *and* value-type references fully qualified as `Application.X` once the same-named `Api.X` types were added — including the DTO type used directly as an MVC action parameter (`[FromBody] Application.UpdateMembersCommand`), not just constructor calls.

**Frontend state found while researching this pass**: `WorkspaceMutations` (`workspace.mutations.ts`) already fully implements the create=backend-first / update=local-first-queued / delete=backend-first-no-queue split — nothing needed there beyond the backend fix above. `member.mutations.ts` and `status.mutations.ts` are still genuine empty stubs (1 line each). There is no `entity-access.mutations.ts` file at all yet. All the relevant MobX stores + IndexedDB wrappers (`memberStore`/`memberDB`, `statusStore`/`statusDB`, `entityAccessStore`/`entityAccessDB`) are already wired in `root.store.ts`, so only the mutation classes are missing on the frontend side.

**Note: `AccessLevel`/`EntityAccess`-based permission checking is being stripped project-wide.** Confirmed directive: `EntityAccess` (private-space, per-member access-level grants) is itself considered legacy now. Task/Space/Folder/Document/DocumentBlock/Status Create/Update/Delete handlers all use `Api.SyncPermissionService` (`server/Api/Common/SyncPermissionService.cs` — `RequireMember()`, `RequireAdmin()`, `RequireCreatorOrAdmin(creatorId)`, pure workspace-role checks, no space privacy/`AccessLevel` at all). `UpdateSpaceStatusesHandler` and `DeleteDocumentHandler` were both migrated off `PermissionService`/`AccessLevel` to match (previously the last two holdouts). `DocumentScopeResolver.cs` is now dead code — nothing calls it anymore, left in place rather than deleted unprompted. **`EntityAccessBatchHandler` is the one deliberate exception** — still uses `PermissionService.VerifyAsync` with `AccessLevel.Editor`, left as-is on purpose since the whole `EntityAccess` feature is being phased out and isn't worth further investment. Don't use `PermissionService`/`AccessLevel` as the reference pattern for any new slice — use `SyncPermissionService`.

**Subtask/Assignee/Favorite pass.** 
- **Subtask needed no new work** — a subtask is just a `ProjectTask` row with `ParentTaskId` set, and `CreateTaskCommand`/`UpdateTaskCommand` already accept `ParentTaskId`. The legacy `CreateSubTask`/`UpdateSubTask`/`DeleteSubTask` commands (`Application/Features/TaskFeatures/SubtaskManagement/`) are narrower, pre-sync-architecture versions of the same thing — left untouched, not ported.
- **Assignee**: built `Api/Features/AssigneeFeatures/{CreateAssignee,DeleteAssignee}` — single-entity (not the legacy diff/changeset `UpdateTaskAssigneesCommand`), matching the rest of the system's one-mutation-per-entity convention. `TaskAssignment.Create()` gained an explicit-id overload (mirroring Comment/Document/DocumentBlock). New `SyncEntityType.Assignee` enum value added. Routes: `POST /api/assignees/sync`, `DELETE /api/assignees/sync/{id}`.
- **Favorite**: built `Api/Features/FavoriteFeatures/{ToggleFavorite,ReorderFavorite}` — **deliberately backend-first with no `SyncEvent`/broadcast at all** (same bucket as Workspace mutations), because Favorite is personal (`WorkspaceMemberId`-scoped) and the SyncHub group broadcasts to the *entire* workspace — broadcasting favorite-toggle events would leak "who favorited what" to every other member. `ToggleFavorite` still uses `IdempotencyService` (a retried toggle without dedup would double-flip the state — unlike other bypasses, it returns the *actual current state* on a trace replay, not a no-op `0`, since the client needs the real result). No `GetFavorites`-equivalent read endpoint was built (matches the established pattern in this migration: Get/list queries aren't part of this port — see §11 gap list). Routes: `POST /api/favorites/toggle`, `PUT /api/favorites/reorder`, no `/sync` suffix (matches `DeleteWorkspaceEndpoint`'s backend-first convention).
- **Notifications** ported too, same read-replica treatment as Workspace: `Api/Features/NotificationFeatures/{FetchNotifications,MarkNotificationsRead}` (`GET /api/notifications/sync`, `PUT /api/notifications/sync/read`), no `SyncEvent`, plain Dapper port of the legacy handlers.
- **OrderKey is purely client-computed now, project-wide** — confirmed mid-session: no handler should resolve `PreviousOrderKey`/`NextOrderKey` into a `FractionalIndex` value server-side anymore. `UpdateSpaceStatusesCommand`'s `StatusUpdateValue` was changed from `PreviousOrderKey`/`NextOrderKey` to a plain `OrderKey` field (client sends the final value directly, like Task/Space/Folder always did). `ReorderFavoriteCommand`/`ToggleFavoriteCommand` were built this way from the start.

**Comment/Document/DocumentBlock slices (added after the initial Task/Space/Folder pass) — same naming-collision gotcha, hit a 4th time.** `Api.DeleteCommentCommand` collided with the legacy `Application.DeleteCommentCommand` referenced unqualified in `TasksController.cs` — fully-qualified it (`new Application.DeleteCommentCommand(id, commentId)`). Design notes: `Comment.Create()` gained an explicit-id overload (mirroring Document/DocumentBlock) since offline-created comments need a client-dictated id. Comment/DocumentBlock deletes are soft-deletes here even though the legacy handlers (`DeleteCommentHandler`, `UpdateDocumentBlocksHandler`) hard-delete — deliberate, so offline clients get a tombstone `D` SyncEvent instead of silently losing reconciliation. DocumentBlock mutations are single-entity (Create/Update/Delete) rather than the legacy bulk `PUT /documents/{id}/blocks` list endpoint, to match the one-SyncEvent-per-entity model the rest of the sync system uses. `DocumentScopeResolver` (new shared helper in `Api/Features/DocumentFeatures/`) resolves a Document's owning space via the same cascading lookup as legacy `UpdateDocumentBlocksHandler` (Space.DefaultDocumentId → Task.DefaultDocumentId → EntityAssetLink), reused across all 5 Document/DocumentBlock handlers that need permission scoping.

## 3. Endpoint registration — NOT automatic by default
Minimal API `app.MapGet/MapPost(...)` calls only take effect if actually invoked. New slices follow the convention: a static class named `XEndpoint` with a `public static void MapEndpoint(IEndpointRouteBuilder app)` method. These are **not** called individually in `Program.cs` — instead `EndpointMappingExtensions.MapAllEndpoints(this IEndpointRouteBuilder app, Assembly assembly)` reflects over the assembly, finds every static class with that exact method signature, and invokes it. Wired in `Program.cs` as `app.MapAllEndpoints(typeof(Program).Assembly)`.

**Gotcha already hit once:** before this existed, `GetBootstrapEndpoint`/`GetChangesEndpoint` compiled fine but were never actually mapped — the routes silently 404'd. If you add a new `XEndpoint.cs` and it's not responding, first check it actually has `MapEndpoint` as a `public static void` method on a `static` class (the reflection scan requires `IsClass && IsAbstract && IsSealed`, i.e. a C# `static class`).

## 4. CRITICAL gotcha — `IActionResult` vs `IResult` in minimal APIs
`Microsoft.AspNetCore.Mvc.IActionResult` (the old MVC type, what `ResultExtensions.ToActionResult()` returns) is **not** properly executed by minimal-API's `RequestDelegateFactory` in this app's setup. Returning it from a `MapGet`/`MapPost` delegate causes the *entire C# `ObjectResult` object* to get JSON-serialized as-is — the response body becomes `{"value": {...actual data...}, "formatters": [], "contentTypes": [], "declaredType": null, "statusCode": 200}` instead of the flat data. This is silent — no exception, 200 status, just garbage nested JSON that breaks every frontend consumer expecting flat data.

**Rule going forward:** new minimal-API endpoints must use `MinimalResultExtensions.ToMinimalResult()` (returns `Microsoft.AspNetCore.Http.IResult` via `Results.Ok(...)`/`Results.Problem(...)`), never `ResultExtensions.ToActionResult()`. The latter stays reserved for the legacy `Controllers/*.cs` files only. `CreateTaskEndpoint` predates this fix and uses raw `Results.Ok(...)`/`Results.BadRequest(...)` inline — also fine, just less consistent; `GetBootstrapEndpoint`/`GetChangesEndpoint` use the new extension method.

## 5. DI registration — handlers/validators must be scanned from BOTH assemblies
`Application/DependencyInjection.cs`'s `AddApplication()` originally only scanned `typeof(ApplicationAssemblyMarker).Assembly` (i.e., `Application.dll`) for `ICommandHandler<>`/`IQueryHandler<,>` implementations and FluentValidation validators. New handlers live in `Api.dll`, so they were never registered — symptom was a **circular dependency exception** on the decorator (`PipelineDecorator.QueryHandler<,>` trying to decorate nothing, since Scrutor found no real implementation to wrap).

**Fixed** by scanning `Assembly.GetEntryAssembly()` (resolves to `Api.dll` at runtime regardless of where the scanning code is defined) in addition to the `Application` assembly — see the `handlerAssemblies` array near the top of `AddApplication()`. Both the `services.Scan(...)` handler registration and the `AddValidatorsFromAssembly` loop now iterate `handlerAssemblies`. If a new handler/validator in `Api` doesn't seem to be picked up, this is the first place to check.

## 6. `sync_events` table — the event log
```
sync_events
  id              bigserial (identity) — this IS the syncId / lastSyncId sequence number
  project_workspace_id  uuid, FK→project_workspaces, cascade delete
  entity_type     text (SyncEntityType enum as string)
  entity_id       uuid
  action          text ('C'/'U'/'D')
  payload         jsonb — full entity snapshot at time of mutation, camelCase keys
  client_trace_id text — matches the frontend's PendingTransaction.id, used for dedup/idempotency
  author_user_id  uuid
  created_at      timestamptz
  is_published    bool — reserved for a future background publish worker, not currently used anywhere
indexes: (project_workspace_id, id) — delta query; is_published — future worker
```
Every mutation handler appends one row per entity it touches inside the same DB transaction as the actual entity write (see `CreateTaskHandler` — writes both a Task row and a Document row, so it appends 2 `sync_events` rows). `processed_traces` (keyed by `trace_id`) is the idempotency ledger — `IdempotencyService.HasProcessedAsync`/`MarkAsProcessed` check/write it inside the same transaction, so retried requests (same `X-Client-Trace-Id`) are detected and skipped before any duplicate writes happen.

## 7. Broadcast — single vs batched
`RealtimeService.NotifySyncEventAsync(workspaceId, payload)` sends one `Delta` SignalR message. `NotifySyncEventBatchAsync(workspaceId, payloads)` sends one `DeltaBatch` message containing all of them (falls back to single `Delta` if given exactly 1 payload). **Always batch when one logical operation produces multiple `sync_events` rows** — `CreateTaskHandler` creates both a Document and a Task, and originally fired two separate `NotifySyncEventAsync` calls (two round-trip messages for one user action); now it builds both payloads and calls `NotifySyncEventBatchAsync` once. Apply this pattern to any future multi-entity-creating handler.

Both broadcast to the `workspace:{workspaceId}` SignalR group on `SyncHub` (a **different** hub registry than the old `WorkspaceHub` — same group-naming convention, but `IHubContext<SyncHub>` and `IHubContext<WorkspaceHub>` are entirely separate broadcast channels; a message sent to one never reaches clients connected to the other).

**Self-echo is required, not excluded — correcting a previous wrong claim here.** `GetSenderConnectionId()` (reads the `X-Connection-Id` header, set by the frontend's axios interceptor from `signalRService.getConnectionId()`) is meant to exclude the sender's own connection from the broadcast — but `signalRService` is the **`WorkspaceHub`** connection, a different SignalR connection entirely from the one `SyncEngine` opens to `SyncHub`. So the connection ID being excluded was never a member of the `SyncHub` group being broadcast to, and the exclusion is a no-op for every sync Delta/DeltaBatch — the sender's own `SyncHub` connection always receives the message. This is not a bug to fix: the whole "a queued transaction stays in the queue until confirmed" design (`applyDelta`'s `rootStore.transactionDB!.dequeue(delta.clientTraceId)`) depends on the creating client receiving its own echo. Without it, every successful mutation would leave its transaction stuck `in_flight` forever. Confirmed via live testing — the creating client's own console shows `Delta received: Comment C <id>` immediately after that client's own create call.

## 8. `SyncHub` — catch-up on every connect
```csharp
OnConnectedAsync():
  read workspaceId + lastSyncId from query string (?workspaceId=X&lastSyncId=Y)
  if workspaceId invalid → Context.Abort()
  join "workspace:{workspaceId}" group
  batch = SyncQueryService.GetChangesAsync(workspaceId, lastSyncId)
  send "DeltaBatch" to Clients.Caller (not the whole group — just this connection)
```
This fires on **every** new physical connection, including SignalR's automatic reconnects (a reconnect is a brand new connection from the server's perspective, not a resume) — so catch-up is automatic and requires no special client-side reconnect logic beyond registering the `DeltaBatch` listener once. No `[Authorize]` attribute on `SyncHub` (matches the existing `WorkspaceHub` convention — neither hub currently enforces auth at the hub level, relying on the broader auth pipeline instead). Worth revisiting before this goes to production.

## 9. Domain rule learned during testing — tasks always belong to a space
Initially `CreateTaskCommand.ProjectSpaceId` was nullable and the permission check / bootstrap query were patched to tolerate `null`. This was **wrong** — confirmed with the user that tasks are not meant to exist at the workspace level; status is a space-layer concept, so a space-less task is invalid domain data, not a real scenario. Reverted:
- `CreateTaskValidator` now has `RuleFor(x => x.ProjectSpaceId).NotEmpty()`
- `CreateTaskHandler`'s permission check simplified back to a single unconditional `VerifyAsync(Role.Member, spaceId: request.ProjectSpaceId, requiredAccess: AccessLevel.Editor, ...)` call (no more null-branching)
- `GetBootstrapHandler`'s task query kept its original `INNER JOIN project_spaces` (briefly changed to `LEFT JOIN` to tolerate the now-invalid null-space test data, then reverted once the real fix — rejecting null spaceId at the source — was in place)
- A handful of orphaned space-less test tasks exist in the dev DB from before this fix; they're invisible to bootstrap now (correctly, since they're invalid) and can be ignored or cleaned up later.

`PermissionService.VerifyAsync(requiredRole, spaceId, requiredAccess, ...)` has a hard contract: `spaceId` and `requiredAccess` must both be null or both be provided — passing one without the other throws `ArgumentException`. Keep this in mind for any future handler with optional space scoping.

## 10. PermissionService — sync `Verify()` for Update*/Delete* handlers
Update and Delete handlers were making two separate queries to the space table: one to load the entity (which joins to space for private/access), and one inside `PermissionService.VerifyAsync()` which re-queried spaces independently. Fixed by loading both in one `Select` projection:

```csharp
// Space handlers: project Space + CallerAccess in one query
var spaceData = await db.ProjectSpaces
    .Where(s => s.Id == request.SpaceId && s.DeletedAt == null)
    .Select(s => new {
        Space = s,
        CallerAccess = db.EntityAccesses
            .Where(ea => ea.ProjectSpaceId == s.Id && ea.WorkspaceMemberId == memberId && ea.DeletedAt == null)
            .Select(ea => (AccessLevel?)ea.AccessLevel).FirstOrDefault()
    })
    .FirstOrDefaultAsync(ct);

// Folder/Task handlers: also pull SpaceIsPrivate as correlated subquery
.Select(t => new {
    Task = t,
    SpaceIsPrivate = db.ProjectSpaces.Where(s => s.Id == t.ProjectSpaceId).Select(s => s.IsPrivate).FirstOrDefault(),
    CallerAccess = db.EntityAccesses...
})
```

`PermissionService` now has a **sync `Verify()`** (pure logic, no DB) alongside the existing `VerifyAsync()` (does a DB query). Update*/Delete* handlers use sync `Verify()` since they already have the space data. Create* handlers keep `VerifyAsync()` since they don't have it yet.

```csharp
// Pure logic — no DB call, takes pre-loaded values
public bool Verify(Role requiredRole, bool isPrivate, AccessLevel? callerAccessLevel, AccessLevel? requiredAccess = null, Guid? creatorId = null)
```

EF Core tracks an entity even when projected inside `Select(e => new { Entity = e, ... })` (EF Core 5+), so `space.Delete()` / `folder.Delete()` etc. still work on the projected entity.

## 11. What's proven vs not — see `SYNC_SCENARIOS.md`
The full scenario-by-scenario checklist (per-entity CRUD, connection lifecycle, conflict edge cases, auth, app integration) now lives in `SYNC_SCENARIOS.md` at the repo root — update it the moment a scenario is built/tested rather than relying on this doc's prose. Quick summary as of last test session:
✅ `CreateTask/UpdateTask/DeleteTask` — online, offline+flush, reconnect catch-up — all manually tested via `/dev/sync-test` + direct Postgres queries
✅ `CreateSpace/UpdateSpace` — online tested
✅ `DeleteSpace` cascade — online + multi-client tested end-to-end (other client receives Space D and cascades children); DB confirms matching `deleted_at` timestamps including statuses
✅ `CreateFolder/UpdateFolder` — online tested
✅ `DeleteFolder` reparent — built + online tested; Task U events with `folderId: null` confirmed in `sync_events`
✅ Bootstrap (Spaces/Folders/Statuses) — `GetBootstrapHandler` returns all four entity types, tested cold-start + force re-bootstrap; stale bootstrap bug fixed on frontend
🔶 Offline Space/Folder delete with pending child ops — `cancelByEntityId` + parent-space guard built and partially exercised, not fully deliberate end-to-end
⬜ Batch flush (`POST /api/sync/batch`) — not built
⬜ Auth on `SyncHub` — currently unguarded like the old `WorkspaceHub`

## 12. Batch flush — `POST /api/sync/batch`
Replaces the current N-sequential-API-calls flush with one HTTP round-trip.

**Request body:**
```json
{ "items": [{ "traceId": "uuid", "entityType": "Task", "action": "C", "entityId": "uuid", "data": {...} }, ...] }
```
Items must arrive in causal order (oldest-first — the client sorts by `createdAt` before sending, same as sequential flush).

**Handler logic:**
```
foreach item in order:
  if IdempotencyService.HasProcessed(item.traceId) → skip (already applied from a prior flush attempt)
  switch item.entityType + item.action:
    Task/C → run CreateTask logic inline (same domain writes as CreateTaskHandler)
    Task/U → run UpdateTask logic inline
    Task/D → run DeleteTask logic inline
    Space/C → run CreateSpace logic inline
    ... etc.
  collect SyncEvent payloads
broadcast all collected SyncEvents as one NotifySyncEventBatchAsync call
return { results: [{ traceId, syncEventId, success, error }] }
```

**Key design rules:**
- Each item still gets its own idempotency check — so a partial flush retry (connection dropped mid-batch) is safe to resend in full.
- **Do NOT stop on first failure** — unlike the sequential flush which `break`s on error, the batch handler should continue processing remaining items and report per-item `success/error` in the response. The client then dequeues successes and leaves failures as `pending`.
- All DB writes happen in **separate transactions per item** — NOT one giant transaction for the whole batch. If item 3 fails, items 1–2 are already committed and shouldn't be rolled back.
- Broadcast is one single `DeltaBatch` at the end covering all successfully-written SyncEvents.
- The handler needs direct access to domain services (same dependencies as the individual handlers) — it cannot simply re-call `IHandler.SendAsync` for each item because that would re-run middleware (WorkspaceContext, IdempotencyMiddleware) redundantly. Process inline with shared services.

**Frontend change:** `TransactionQueue.flush()` replaces the `for` loop with a single `POST /api/sync/batch` call. `SyncEngine.sendBatch(txs[])` replaces `sendTransaction(tx)`. On response, dequeue all `traceId`s where `success: true`; leave failures as `pending`. SignalR still delivers the DeltaBatch confirmation as usual.

## 13. Local dev setup notes
- EF migrations need `TASKPLAN_CONNECTION_STRING` env var set (design-time factory has no other fallback locally) — see `DesignTimeDbContextFactory.cs`.
- `appsettings.Development.json` now has `AppSettings:FrontendUrl/BackendUrl` = `https://localhost:5173`/`https://localhost:7285` and `CookieSettings:Domain` = `""` (host-only cookie) — these were emptied during an earlier production-deploy cleanup pass and had to be restored for local dev to work (cookies wouldn't persist, login appeared to "succeed" then bounce back to sign-in).
