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
3. `space.Delete()` soft-deletes the space itself
4. A **single Space D SyncEvent** is emitted and broadcast via `NotifySyncEventAsync`

The client that receives Space D is responsible for cascading removal of all children locally (see `FRONTEND_SYNC_CONTEXT.md §9` — delta-handler Space D case). This is deliberate: emitting one event per child entity would create O(n) SignalR messages and require the client to process them individually, when a single Space D with client-side cascade is equivalent and far cheaper.

**`DeleteFolder` — reparent tasks, then tombstone.** Backend emits N Task U events (one per orphaned task, with `folderId: null`) + one Folder D event, broadcast as a single `NotifySyncEventBatchAsync` call. The client initiating the delete reparents tasks locally immediately; other clients receive the Task U deltas and reparent via the normal upsert path.

**Folder slices** follow the same naming-collision gotcha as Space — had to fully-qualify `Application.CreateFolderCommand` etc. in the legacy `FolderController.cs` when the new `Api`-namespaced commands were added.

**Same naming-collision gotcha as Task** — had to fully-qualify `Application.CreateSpaceCommand`/`UpdateSpaceCommand`/`DeleteSpaceCommand` in the legacy `SpacesController.cs` (`server/Api/Controllers/SpaceController.cs`) once the new `Api`-namespaced commands of the same name were added. This is now the third time this exact issue has been hit (Task, then Task again for Update/Delete, now Space) — expect it every time a new entity gets sync slices; check the corresponding legacy `*Controller.cs` first.

**Frontend note:** `delta-handler.ts`'s `getEntityApplier()` only handles `"Task"` and `"Document"` cases today — `Space`, `Status`, and `EntityAccess` deltas will hit the `default` branch and get dropped with a `console.warn`. The backend slices are real and tested at the DB level, but nothing client-side stores Space/Status/EntityAccess data yet. That's expected — backend-first per the agreed plan — but don't be surprised when `/dev/sync-test` shows no visible effect for spaces until the frontend catches up.

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

Both broadcast to the `workspace:{workspaceId}` SignalR group on `SyncHub` (a **different** hub registry than the old `WorkspaceHub` — same group-naming convention, but `IHubContext<SyncHub>` and `IHubContext<WorkspaceHub>` are entirely separate broadcast channels; a message sent to one never reaches clients connected to the other). `GetSenderConnectionId()` (reads `X-Connection-Id` header) excludes the sender's own connection from the broadcast — though in practice the frontend also self-filters via `clientTraceId` matching its own pending transaction, so this is belt-and-suspenders.

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

## 10. What's proven vs not — see `SYNC_SCENARIOS.md`
The full scenario-by-scenario checklist (per-entity CRUD, connection lifecycle, conflict edge cases, auth, app integration) now lives in `SYNC_SCENARIOS.md` at the repo root — update it the moment a scenario is built/tested rather than relying on this doc's prose. Quick summary as of last test session:
✅ `CreateTask/UpdateTask/DeleteTask` — online, offline+flush, reconnect catch-up — all manually tested via `/dev/sync-test` + direct Postgres queries
✅ `CreateSpace/UpdateSpace` — online tested
✅ `DeleteSpace` cascade — online tested for both initiator and receiving client (matching `deleted_at` timestamps in DB confirm bulk ExecuteUpdateAsync fired atomically)
✅ `CreateFolder/UpdateFolder` — online tested
✅ `DeleteFolder` reparent — built + online tested; Task U events with `folderId: null` confirmed in `sync_events`
🔶 Bootstrap (Spaces/Folders/Statuses) — queries added to `GetBootstrapHandler`, builds clean, not manually tested cold-start end-to-end
🔶 Offline Space/Folder delete with pending child ops — frontend `cancelByEntityId` logic built, not deliberately tested
⬜ Batch flush (`POST /api/sync/batch`) — not built
⬜ Auth on `SyncHub` — currently unguarded like the old `WorkspaceHub`

## 11. Batch flush — `POST /api/sync/batch`
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

## 12. Local dev setup notes
- EF migrations need `TASKPLAN_CONNECTION_STRING` env var set (design-time factory has no other fallback locally) — see `DesignTimeDbContextFactory.cs`.
- `appsettings.Development.json` now has `AppSettings:FrontendUrl/BackendUrl` = `https://localhost:5173`/`https://localhost:7285` and `CookieSettings:Domain` = `""` (host-only cookie) — these were emptied during an earlier production-deploy cleanup pass and had to be restored for local dev to work (cookies wouldn't persist, login appeared to "succeed" then bounce back to sign-in).
