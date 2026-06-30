# Backend Sync Architecture & Context

*Use this document as context for future chats to quickly spin up AI assistants on the current state of the backend sync system.*

**Companion docs:** `FRONTEND_SYNC_CONTEXT.md` (repo root) covers the client side of this same system. `SYNC_SCENARIOS.md` (repo root) is the living checklist of what's actually built/tested vs not — check it before assuming a scenario works.

## 1. Where new code lives — `Api` project, not `Application`
The existing app is vertical-slice architecture inside the `Application` project (legacy, untouched, still serves the old REST/Redux/RTK-Query frontend on the old `feature/sync-engine` branch's predecessor). **All new sync-system code goes in the `Api` project** as minimal-API slices, deliberately separate from the old `Application`-layer Controllers.

```
Api/Features/
  TaskFeatures/CreateTask/        — CreateTaskCommand, CreateTaskHandler, CreateTaskValidator, CreateTaskEndpoint
  TaskFeatures/UpdateTask/        — empty scaffold, not built
  SyncFeatures/Bootstrap/         — GetBootstrapQuery, GetBootstrapHandler, GetBootstrapEndpoint
  SyncFeatures/GetChanges/        — GetChangesQuery, GetChangesHandler, GetChangesEndpoint
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

When adding `UpdateTask`/`DeleteTask`: use `PUT /api/tasks/sync/{id}` and `DELETE /api/tasks/sync/{id}` — the frontend's `SyncEngine.getRequestConfig()` already assumes this convention.

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
✅ `CreateTask` — online, offline+flush, reconnect catch-up, batched broadcast, Document delta — all manually tested via `/dev/sync-test` + direct Postgres queries against `sync_events`
⬜ `UpdateTask`/`DeleteTask` — not built (`Api/Features/TaskFeatures/UpdateTask/` is an empty folder)
⬜ Any entity besides Task/Document — no handlers, no bootstrap support
⬜ Bootstrap — Task-only; Spaces/Folders/etc. need their own queries added to `GetBootstrapHandler`/`BootstrapResult`
⬜ Auth on `SyncHub` — currently unguarded like the old `WorkspaceHub`

## 11. Local dev setup notes
- EF migrations need `TASKPLAN_CONNECTION_STRING` env var set (design-time factory has no other fallback locally) — see `DesignTimeDbContextFactory.cs`.
- `appsettings.Development.json` now has `AppSettings:FrontendUrl/BackendUrl` = `https://localhost:5173`/`https://localhost:7285` and `CookieSettings:Domain` = `""` (host-only cookie) — these were emptied during an earlier production-deploy cleanup pass and had to be restored for local dev to work (cookies wouldn't persist, login appeared to "succeed" then bounce back to sign-in).
