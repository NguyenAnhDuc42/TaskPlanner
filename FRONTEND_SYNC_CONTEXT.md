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

- **Read-Replica Entities (Workspaces, Notifications):**
  These **bypass** the Sync Engine because their mutations require strict backend logic (e.g., joining a workspace).
  - They use standard `api.post` calls with loading spinners.
  - Upon success, the frontend manually updates the MobX store and IndexedDB cache.

## 2. Storage Layers
- **IndexedDB (Persistence):** Uses `idb`. There are two databases:
  1. `UserDB` (`db/user-schema.ts`): Stores global entities (`workspaces`, `notifications`). Opens on `initUser()`.
  2. `TaskPlanDB` (`db/schema.ts`, `DB_VERSION = 2`): Stores workspace-specific entities — `tasks`, `spaces`, `folders`, `comments`, `statuses`, `documents`, `document_blocks`, `entity_access`, `members`, plus `__metadata` and `__transactions`. Opens on `switchWorkspace()`.
  - The `upgrade()` callback is fully idempotent — every `createObjectStore` call is guarded with `if (!db.objectStoreNames.contains(...))`. Always keep it that way: bumping `DB_VERSION` without guards will throw `ObjectStore already exists` for any browser that already has an earlier version.

- **MobX (Memory/Reactivity):** Centered around `RootStore` (`stores/root.store.ts`).
  - One MobX store per entity type (`TaskStore`, `SpaceStore`, `FolderStore`, `MemberStore`, `StatusStore`, `CommentStore`, `DocumentStore`, `DocumentBlockStore`, `EntityAccessStore`, `WorkspaceStore`, `NotificationStore`).
  - When `switchWorkspace()` is called, all workspace-specific stores are cleared and hydrated in parallel from IndexedDB via `hydrateFromLocal()`.
  - `RootStoreProvider`/`useStore()` context exists but is **not yet mounted anywhere in the real app** — only the standalone test page constructs its own `RootStore` instance directly.

## 3. Folder Structure
```
src/
  db/
    schema.ts              — TaskPlanDB (workspace-scoped) schema + openWorkspaceDB/closeWorkspaceDB
    user-schema.ts          — UserDB (global) schema + openUserDB/closeUserDB
    index.ts                — re-exports everything in operations/
    operations/*.db.ts      — one class per entity (TaskDB, SpaceDB, DocumentDB, ...), CRUD over one IDB store

  stores/
    root.store.ts            — RootStore: owns all MobX stores + DB wrapper instances, switchWorkspace()/initUser()
    *.store.ts                — one MobX store per entity (observable.map + upsert/remove/hydrate/clear)

  sync/
    sync-engine.ts           — SyncEngine: init/bootstrap/connect/disconnect, owns the SignalR connection + TransactionQueue
    delta-handler.ts         — applyDelta/applyDeltaBatch: routes a Delta to the right store+DB via getEntityApplier()
    transaction-queue.ts     — TransactionQueue: enqueue/dequeue/flush/recoverInFlight over __transactions

  mutations/
    task.mutations.ts         — TaskMutations.create/update/delete — the only one wired up so far
    (space/folder/member/status/document/workspace .mutations.ts exist as empty scaffolding)

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

See `SYNC_SCENARIOS.md` for the full per-scenario status checklist (CRUD per entity, connection lifecycle, conflict edge cases, auth, app integration).

## 8. Unsolved Edge Cases / Future Work
- **Transaction Squashing (Offline Edits):** If a user edits a task 5 times while offline, the queue currently has 5 separate `PUT` transactions. The backend will process them sequentially. In the future, the `TransactionQueue` could squash redundant updates for the same `entityId` before sending.
- **Update/Delete mutations** — `TaskMutations.update`/`.delete` exist client-side (same enqueue/online/offline pattern as create) but have no backend slice yet (`/tasks/sync/{id}` PUT/DELETE return nothing — routes don't exist). Backend `Api/Features/TaskFeatures/UpdateTask/` folder exists but is empty.
- **Other entity types** (Space, Folder, Member, Status, Comment, Document, DocumentBlock, EntityAccess) — DB/store scaffolding exists for all, but only Task has a working mutation + backend slice. `mutations/*.mutations.ts` for the others are empty stub files.
- **Bootstrap is Task-only** — `GetBootstrapHandler` (backend) only returns Tasks. Spaces/Folders/etc. need their own bootstrap queries added before those entities can cold-load.
- **`RootStoreProvider` not mounted in the real app** — the whole sync system today only runs inside the isolated test page. Wiring it into the actual app (replacing/coexisting with the old Redux/RTK Query system) is unstarted.
