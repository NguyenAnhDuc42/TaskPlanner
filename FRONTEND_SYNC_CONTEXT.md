# Frontend Sync Architecture & Context

*Use this document as context for future chats to quickly spin up AI assistants on the current state of the frontend architecture.*

**Companion docs:** `BACKEND_SYNC_CONTEXT.md` (repo root) covers the server side of this same system. `SYNC_SCENARIOS.md` (repo root) is the living checklist of what's actually built/tested vs not — check it before assuming a scenario works.

## 1. Core Architecture Pattern: Hybrid Local-First
The frontend uses a hybrid approach to balance zero-latency UI (optimistic updates) with strict backend authority.

- **Optimistic Entities (Tasks, Folders, Spaces, Comments, Documents, DocumentBlocks):**
  These use the **Sync Engine**. When a user mutates these, the UI updates instantly (`RootStore` -> `IndexedDB`). A transaction is queued in the `TransactionQueue` and a synchronous API call is fired (skipped if offline).
  - If the API succeeds: The transaction waits in the queue until SignalR confirms it via a broadcast `Delta`/`DeltaBatch`.
  - If the API fails (Network Error / offline): The transaction sits in the queue to be retried on reconnect or manual flush.
  - If the API fails (400/403 Server Rejection): The UI rolls back the change immediately and dequeues the transaction.

- **Read-Replica / backend-first Entities (Workspaces, Notifications, Favorites, Status, Member):**
  These **bypass** `TransactionQueue` — either because their mutations require strict backend logic (joining a workspace), because broadcasting would leak personal data (Favorites are per-member; a workspace-wide SyncEvent would tell everyone who favorited what), or because there's simply no `BatchFlushHandler` case for them (Status, Member) so a queued mutation could never actually be replayed offline.
  - They call `api.post`/`api.put` directly, apply an optimistic update where one is possible (not always — e.g. `MemberMutations.add()` can't optimistically add a member since the client never knows the real `WorkspaceMemberId` until the server resolves the email), and roll back everything on failure.
  - Status/Member/Notification's Update/Remove *do* still emit a `SyncEvent`/Delta for everyone else — only the *sending* client's own path skips the queue.

## 1a. Favorites — a cautionary tale on where personal data should live
Favorites were originally built as `isFavorite`/`favoriteOrderKey` fields bolted onto `TaskRecord`/`FolderRecord`/`SpaceRecord` themselves. This was wrong, and cost a long chain of bugs: `delta-handler.ts` applies every incoming Task/Folder/Space Delta via a **full-replace** `upsert()`, and since favorite fields are deliberately never present in a Task/Folder/Space SyncEvent payload, *any* unrelated update to a favorited entity — by anyone, anywhere — silently wiped its favorite state. Fixed by giving favorites their own `FavoriteStore`/`FavoriteDB`, keyed by `entityId`, completely decoupled from the entities they point at. **Lesson for any future personal/per-member data**: never attach it as a field on a record the sync engine's Delta system owns — give it its own store.

## 2. Storage Layers
- **IndexedDB (Persistence):** Uses `idb`. There are two databases:
  1. `UserDB` (`db/user-schema.ts`): Stores global entities (`workspaces`, `notifications`). Opens on `initUser()`.
  2. `TaskPlanDB` (`db/schema.ts`, `DB_VERSION = 4`): Stores workspace-specific entities — `tasks`, `spaces`, `folders`, `comments`, `statuses`, `documents`, `document_blocks`, `entity_access`, `members`, `assignees`, `favorites`, plus `__metadata` and `__transactions`. Opens on `switchWorkspace()`.
  - The `upgrade()` callback is fully idempotent — every `createObjectStore` call is guarded with `if (!db.objectStoreNames.contains(...))`. Always keep it that way: bumping `DB_VERSION` without guards will throw `ObjectStore already exists` for any browser that already has an earlier version.
  - **`favorites`' keyPath is `entityId`, not `id`** — one favorite per entity per member, so entity id is the natural key; the favorite row's own server-side id is carried as a field but never used for lookups.

- **MobX (Memory/Reactivity):** Centered around `RootStore` (`stores/root.store.ts`).
  - One MobX store per entity type (`TaskStore`, `SpaceStore`, `FolderStore`, `MemberStore`, `StatusStore`, `CommentStore`, `DocumentStore`, `DocumentBlockStore`, `EntityAccessStore`, `AssigneeStore`, `FavoriteStore`, `WorkspaceStore`, `NotificationStore`).
  - When `switchWorkspace()` is called, all workspace-specific stores are cleared and hydrated in parallel from IndexedDB via `hydrateFromLocal()`.
  - **`RootStore` is now created once at the app root** (`routes/__root.tsx`, above all routing) — not inside `SyncProvider` anymore. This matters because user-level state (`notificationStore`, `workspaceStore`) needs to exist both inside a workspace and on the pre-workspace home screen (`NotificationBell` renders in both places); creating `RootStore` per-workspace inside `SyncProvider` would wipe that state every time you left a workspace. `SyncProvider` now just calls `switchWorkspace()`/`init()` on the shared instance via `useStore()`. `RootStoreProvider`/`useStore()` are mounted for real across the whole app now, not just the standalone test page (which still constructs its own separate, disconnected `RootStore` for isolated testing).
  - `Store.upsert()` for entities that are Delta targets (Task/Folder/Space/Status/etc.) should generally be a plain replace, **except** if a field on that record is deliberately kept outside the Delta payload (there currently are none — Favorite was the one exception and has since been moved to its own store). If a future personal/local-only field ever gets added directly to one of these records, `upsert()` would need to merge instead of replace to avoid the same bug Favorites had — see §1a.

## 3. Folder Structure
```
src/
  db/
    schema.ts              — TaskPlanDB (workspace-scoped) schema + openWorkspaceDB/closeWorkspaceDB
    user-schema.ts          — UserDB (global) schema + openUserDB/closeUserDB
    index.ts                — re-exports everything in operations/
    operations/*.db.ts      — one class per entity (TaskDB, SpaceDB, FolderDB, DocumentDB, ...), CRUD over one IDB store

  stores/
    root.store.ts            — RootStore: owns all MobX stores + DB wrapper instances, switchWorkspace()/initUser()
    *.store.ts                — one MobX store per entity (observable.map + upsert/remove/hydrate/clear)

  sync/
    sync-engine.ts           — SyncEngine: init/bootstrap/connect/disconnect, owns the SignalR connection + TransactionQueue
                               forceBootstrap(workspaceId) added — bypasses lastSyncId>0 skip for dev/testing
    delta-handler.ts         — applyDelta/applyDeltaBatch: routes a Delta to the right store+DB via getEntityApplier()
                               getEntityApplier() takes optional cancelByEntityId callback — called on every D action
                               Space D case cascades: dbDelete cancels children's pending txs + deletes from all child DBs;
                               remove() cascades children out of all MobX stores before removing the space itself
    transaction-queue.ts     — TransactionQueue: enqueue/dequeue/flush/recoverInFlight/cancelByEntityId/squash

  mutations/
    task.mutations.ts         — TaskMutations.create/update/delete — fully implemented
    space.mutations.ts        — SpaceMutations.create/update/delete — fully implemented
    folder.mutations.ts       — FolderMutations.create/update/delete — fully implemented
    status.mutations.ts       — StatusMutations.updateBatch — backend-first, no queue (no BatchFlushHandler case exists for Status)
    member.mutations.ts       — MemberMutations.add/update/remove — backend-first, no queue; add() has no optimistic step (can't generate a WorkspaceMemberId client-side)
    favorite.mutations.ts     — FavoriteMutations.toggle/reorder — backend-first, no queue, no SyncEvent at all (personal data); operates on FavoriteStore/FavoriteDB, not the entity stores
    notification.mutations.ts — NotificationMutations.fetchInitial/fetchPage/markRead — backend-first, no queue; markRead is deliberately fire-and-forget (read state isn't critical)
    comment.mutations.ts      — CommentMutations — full optimistic+queue+immediate-send, wired into TaskComments
    assignee.mutations.ts     — AssigneeMutations.create/delete — full optimistic+queue+immediate-send, wired into TaskAssignees
    workspace.mutations.ts    — WorkspaceMutations (server-first, no queue — see §10)
    document.mutations.ts     — DocumentMutations (Update/Delete only) — not yet wired into any UI
    (entity-access.mutations.ts does not exist yet)

  types/sync/
    delta.ts                 — DeltaPayload, DeltaBatchPayload, SyncAction ('C'|'U'|'D'), SyncEntityType (string union)
    transaction.ts            — PendingTransaction, TransactionStatus
    metadata.ts               — WorkspaceMetadata (firstSyncId, lastSyncId, databaseVersion, bootstrappedAt)

  routes/dev/sync-test.tsx    — standalone manual test page, see §6
```

## 4. Naming convention
`SyncEntityType` (not `EntityType`) is used everywhere in the sync system, on both frontend and backend, to avoid a real collision with `Domain.EntityType` (an unrelated notification-entity enum) on the backend. Keep this name on both sides.

## 5. CreateTask flow (the only fully-proven path so far)
```
TaskMutations.create(data: { name, spaceId, ... })
  1. generate id + defaultDocumentId (crypto.randomUUID()) — client dictates both IDs
  2. build `record` (TaskRecord shape — local/display shape)
  3. optimistic upsert into TaskStore
  4. persist `record` to IndexedDB (taskDB.put)
  5. build `commandPayload` (CreateTaskCommand wire shape — projectWorkspaceId, slug, orderKey, defaultDocumentId, etc.)
     — IMPORTANT: built BEFORE enqueue, and `commandPayload` (not `record`) is what gets enqueued.
       record and commandPayload are different shapes for different purposes; conflating them was a real bug
       that broke offline-deferred sends (see §7).
  6. enqueue `commandPayload` into TransactionQueue (so offline-deferred sends use the same shape as immediate sends)
  7. if offline: stop here, transaction stays pending
  8. if online: POST /tasks/sync with commandPayload + X-Client-Trace-Id header = tx.id
     - success: transaction stays in queue until SignalR Delta/DeltaBatch confirms it (NOT dequeued on HTTP 200)
     - network error: leave in queue, don't rollback
     - 4xx rejection: rollback store+db, dequeue transaction, rethrow
```
SignalR (`/hubs/sync`) pushes `Delta` (single) or `DeltaBatch` (multiple — used for both server-side batched broadcasts and reconnect catch-up) events. `delta-handler.ts` applies each to the right store+DB, dequeues the matching transaction by `clientTraceId`, and advances `__metadata.lastSyncId`.

## 6. Manual test page — `/dev/sync-test`
Self-contained — constructs its own `RootStore`/`SyncEngine`/`TaskMutations`, doesn't touch the real app's auth/routing. Requires being logged into the real app first (shares cookies).

Fields: Workspace ID, Space ID (**required** — tasks must belong to a space, validated both client and server side), Task name. Buttons: Connect, Create Task, Go Offline/Online (manually overrides `rootStore.isOnline` — note real browser `online`/`offline` events can still override this independently, see `RootStore` constructor), Flush Queue (manually calls `syncEngine.flushQueue()`, useful since toggling the manual online flag doesn't trigger a real SignalR reconnect).

Validated via this page + direct Postgres queries against `sync_events`:
- ✅ Create while online → optimistic → POST → sync_events written → DeltaBatch (or Delta) received → transaction dequeued
- ✅ Create while offline → queued in `__transactions`, no API call attempted → flush after going online → succeeds
- ✅ Reconnect (simulated via DevTools network throttle) → `SyncHub.OnConnectedAsync` re-sends `DeltaBatch` catch-up automatically, logged client-side as `[SyncEngine] DeltaBatch received: N events`
- ✅ `Document` delta case applies correctly (was previously silently dropped — fixed)

Not yet tested: Update, Delete (not built on backend yet).

## 7. Bugs found and fixed during testing (don't reintroduce)
1. **Offline payload shape bug** — `TaskMutations.create()` enqueued `record` (TaskRecord/local shape) instead of `commandPayload` (wire shape). Immediate online sends used the correct shape, but offline-deferred sends replayed the wrong one and got `400`. Fixed by building `commandPayload` before enqueue and enqueueing that.
2. **`Document` SyncEntityType case missing** in `delta-handler.ts`'s `getEntityApplier` — silently dropped every Document delta (console.warn only). Now handled; `documents` IDB store + `DocumentStore` + `DocumentDB` added (`DB_VERSION` bumped 1→2).
3. **Naming cleanup** — `getModelApplier`→`getEntityApplier`, `TransactionDB.getByModel`→`getByEntity`, `EntityType`→`SyncEntityType` (frontend-only rename, see §4).
4. Test page itself had a stale-closure route-declaration-order bug (`SyncTestPage` referenced before declaration) — fixed by moving the `Route` export below the component.
5. **`devLog` not imported in `delta-handler.ts`** — added debug logging with `devLog(...)` calls inside `getEntityApplier` closures without importing `devLog` from `./dev-log`. This caused `ReferenceError: devLog is not defined` **inside the async `dbDelete` promise**, which was silently swallowed as an unhandled rejection — the cascade never ran, spaces/folders/tasks were never removed from the store. `applyDelta`/`applyDeltaBatch` have no try-catch, so any throw inside an applier silently aborts the entire delta processing. Lesson: always add error boundaries around `applyDelta` calls, and never use bare function names inside closures without verifying the import.
6. **`cancelByEntityId` missed in-flight transactions** — only called `getPending()` (status=`pending`), so transactions already `markInFlight()` (in-flight HTTP firing) were invisible to it. When a space was deleted by another client, the in-flight task update for a child entity was already past the cancellation window. Fixed: now calls both `getPending()` + `getInFlight()` and dequeues all. The echo is additionally blocked by the parent-space guard (see §9).
7. **SignalR `withAutomaticReconnect()` default stops at 4 retries** — default policy retries at 0, 2, 10, 30 seconds then permanently disconnects. After the 4th failed attempt, `onclose` fires and no further reconnections happen — the client misses all deltas until page refresh. Fixed: `withAutomaticReconnect({ nextRetryDelayInMilliseconds: () => 5000 })` retries forever every 5 seconds (matches the existing `signalr-service.ts` workspace hub policy).
8. **Bootstrap `putMany` accumulates stale IndexedDB records** — `bootstrap()` called `putMany()` (upsert, not replace) then read back with `getAll()`. If IndexedDB already had records from old delta events (e.g. entities created then deleted in a previous session), they survived the bootstrap and `hydrate()` loaded all of them. `forceBootstrap()` now clears all four entity DBs before `putMany`. Normal `bootstrap()` (first-time `lastSyncId=0` path) is unaffected — it starts from an empty DB.

See `SYNC_SCENARIOS.md` for the full per-scenario status checklist (CRUD per entity, connection lifecycle, conflict edge cases, auth, app integration).

## 8. Unsolved Edge Cases / Future Work
- **Batch flush — implemented on both sides, not yet load-tested.** `TransactionQueue.flush()` squashes then sends everything in one `POST /api/sync/batch` call via `SyncEngine.sendBatch(txs[])`. `BatchFlushHandler` (backend) covers Task/Space/Folder/Comment/Document/DocumentBlock/Assignee/Workspace(Update). **Status/Member/Favorite/Notification deliberately do NOT go through this** — there's no `BatchFlushHandler` case for any of them, so their mutations classes are backend-first with manual rollback instead (see §1a and the mutations list in §3).
- **`RootStore` mounting — resolved.** `RootStore` now lives at the app root (`routes/__root.tsx`), not inside `SyncProvider`. The sync system runs across the whole real app now (hierarchy sidebar, board, task/space/folder views, members, statuses, favorites, notifications) — not just the isolated test page. See `SYNC_SCENARIOS.md`'s "App integration" section.
- **`sync-engine.ts` staleness detection — new this session.** `init()` now force-re-bootstraps not just when `lastSyncId === 0` but also when the session's stored `databaseVersion` is behind `EXPECTED_DATABASE_VERSION` (a frontend constant that must be bumped in lockstep with the backend's `SyncQueryService.CurrentDatabaseVersion` every time Bootstrap's payload shape changes). Before this existed, adding a new entity type to Bootstrap silently never reached any existing session — Delta catch-up can't backfill something that didn't exist in past SyncEvents. This is exactly what happened when Members were added to Bootstrap and existing users saw an empty member list.
- **Queue not auto-flushed when `isOnline` flag toggles** — the test page's "Go Online" button sets `rootStore.isOnline = true` but does NOT trigger a queue flush. Flush only happens automatically on `connect()` (initial) and `onreconnected` (after real network reconnect). If using the manual flag to simulate offline, the user must click "Flush Queue" manually. There is no MobX reaction wired to auto-flush when `isOnline` transitions to `true`. In the real app this matters: a component toggling `isOnline` won't automatically drain the queue.
- **`isOnline` only reflects the browser's network adapter, not backend reachability** — a downed backend with a healthy network connection never trips `isOnline`, so mutations attempt the real API call and hit whatever a reverse proxy (Vite dev proxy included) synthesizes for an unreachable upstream — typically a 502/503/504 **with a real HTTP response**, not a raw connection error. Every mutation's offline-detection (`!err.response`) used to miss this entirely, wrongly treating "backend down" as "server rejected this" and rolling back. Fixed by adding a shared `isConnectivityError()` helper (`frontend/src/lib/is-connectivity-error.ts`) that also treats 502/503/504 as connectivity loss, used everywhere the old `axios.isAxiosError(err) && !err.response` check used to be.
- **`EntityAccess` mutations** — backend slice exists (`PUT /api/entity-access/sync/batch`) but no `entity-access.mutations.ts` on the frontend. Low priority — `EntityAccess`/private-space access-level checking is being phased out project-wide (see `BACKEND_SYNC_CONTEXT.md` §"AccessLevel/EntityAccess-based permission checking is being stripped").
- **`Document`/`DocumentBlock` mutations** — backend slices + `delta-handler.ts` cases exist, `document.mutations.ts`/`document-block.mutations.ts` are built, but neither is wired into any real UI component yet (the block editor is still Redux/legacy).

## 9. Queue Squashing Rules — IMPLEMENTED
`TransactionQueue.squash()` runs inside `flush()` before any network calls. Groups pending transactions by `entityId`, applies rules in order, returns `{ toSend, toCancel }`. Cancelled IDs are dequeued from IndexedDB first, then `toSend` is flushed sequentially. Causal order is preserved via the first-occurrence timestamp of each entity group.

Rules (oldest action first per `entityId`):

1. **C + D → cancel both.** Entity was created and deleted before ever reaching the server — nothing to tell the server, zero network calls. Both removed from queue.
2. **D beats all U.** Any `U` transactions for an entity that also has a `D` are cancelled — no point updating something about to be deleted. Only the `D` is sent.
3. **C + U(s) → merge into one C.** Update payloads are merged (spread) into the create payload; a single `C` is sent with the final state. Server never sees a create followed by redundant updates for something it hasn't seen yet.
4. **U + U(s) → merge into one U.** Last-write-wins per field via object spread. Single `U` sent.
5. **Delete is locally eager.** All three delete mutations (`TaskMutations`, `SpaceMutations`, `FolderMutations`) remove from store + IndexedDB immediately on delete, before the API call returns. The incoming Delta confirmation is a no-op (entity already gone).

**`cancelByEntityId(entityId)`** — method on `TransactionQueue`. Cancels all pending AND in-flight txs for a given entity ID without flushing. Used in two places:
- `SpaceMutations.delete()` — cancels pending child ops (folders, tasks) before removing the space locally
- `delta-handler.ts` `applyDelta()` D case — cancels pending ops for the deleted entity on any client that receives a D delta, preventing queue-jam on 404 when another user deletes something you had edits queued for. Also called for Space D children inside `dbDelete` (Space cascade).

**Important:** even with `cancelByEntityId`, an in-flight HTTP request that was already fired cannot be un-sent. The server may echo a Delta back for a task that the cascade already removed. The **parent-space guard** in `applyDelta` handles this: for `C`/`U` deltas on Task, Folder, or Status, if the `data.spaceId` is no longer in `spaceStore` (because the cascade already removed it), the upsert is skipped and the entity is also deleted from IndexedDB. This prevents ghost entities from reappearing after a cascade-delete race.

**Cascade delete behavior:**
- **Space D (initiator):** `SpaceMutations.delete()` cancels pending child txs → removes folders/tasks/statuses from all stores+DBs → removes space → enqueues D → calls API
- **Space D (other client, via delta):** `delta-handler.ts` Space D `dbDelete` cancels child pending txs → deletes from all child IndexedDBs; `remove()` cascades children out of all MobX stores
- **Folder D (initiator):** `FolderMutations.delete()` reparents tasks (`folderId = null`) in store+DB → removes folder → enqueues D → calls API. Backend emits Task U events with `folderId: null` for other clients.
- **Folder D (other client):** Task U deltas arrive separately from backend (reparenting), processed by normal upsert path. `applyDelta` D case cancels any pending Folder U/C ops on this client.
- **Task D:** Eager removal from store+DB. Delta D cancels any pending Task ops on other clients.

**Tested end-to-end:**
- Space D cascade (online, other client receives) — ✅ space + folder + task + statuses removed from store and IDB on Delta arrival
- Space D cascade (offline → reconnect) — ✅ DeltaBatch catch-up applies cascade on reconnect; parent-space guard blocks echo re-insertion
- `cancelByEntityId` for in-flight ops — ✅ partially exercised (server returned 400 for task update sent to deleted space; client guard prevented re-insertion)

**Open/not yet tested:**
- Queue squashing scenarios (C+D cancel, D beats U, C+U merge, U+U merge) — logic is in `TransactionQueue.squash()` but not tested end-to-end with actual offline sequences via `/dev/sync-test`

## 10. Workspace mutations — server-first, no queue
Workspace data lives in `UserDB` (not `TaskPlanDB`), so it never goes through the `TransactionQueue` (which is in `TaskPlanDB`). Pattern:
- `create()` / `update()` — call API directly, on success manually update `workspaceStore` + `workspaceDB`
- No optimistic update, no queue entry, no Delta — the workspace list doesn't need sub-second latency
- `workspace.mutations.ts` `update()` currently still calls `transactionQueue.enqueue()` — this is a bug, tracked in §8
