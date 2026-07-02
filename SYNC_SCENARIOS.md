# Sync Engine — Scenario Checklist

*Living tracker. Update the status the moment a scenario is built/tested, not after. This is the source of truth for "is X actually done" — don't trust memory or assume from code existing.*

**Companion docs:** `FRONTEND_SYNC_CONTEXT.md`, `BACKEND_SYNC_CONTEXT.md` (repo root).

Status legend: ✅ built + manually tested · 🔶 built, not tested · ⬜ not built

## Per-entity CRUD (Task)
| Scenario | Status | Notes |
|---|---|---|
| Create (online) | ✅ | tested via `/dev/sync-test` + Postgres check on `sync_events` |
| Create (offline → flush) | ✅ | queued in `__transactions`, flushed after reconnect — re-confirmed in "big queue create off" batch test |
| Update (online) | ✅ | verified directly via Postgres: name change committed (`project_tasks.updated_at` bumped), clean `action='U'` row in `sync_events` |
| Update (offline → flush) | ✅ | "big queue update off" test — queued offline, flushed, single `U` event committed correctly |
| Delete (online) | ✅ | verified via Postgres: `deleted_at` set, clean `action='D'` row in `sync_events` right after the `C` row for that task |
| Delete (offline → flush) | ✅ | "new task for delete offline" test — queued offline, flushed, `deleted_at` committed correctly |
| Date-clearing on Update (`ClearStartDate`/`ClearDueDate`) | ⬜ | fields exist on `UpdateTaskCommand` (backend) but `TaskMutations.update()` never sends them client-side — unimplemented |
| Duplicate Delete on same entity (double-click) | 🔶 | squash `D` beats `D` — only one D sent now that squashing is implemented. The original resurrection bug (rollback re-inserting a deleted entity) is prevented because a second D for the same entityId is dropped by `squash()` before flush. Not explicitly re-tested since squashing was added. |

## Per-entity CRUD (Space)
| Scenario | Status | Notes |
|---|---|---|
| Space Create (online) | ✅ | tested via `/dev/sync-test` |
| Space Update (online) | ✅ | tested via `/dev/sync-test` |
| Space Delete — initiator (online) | ✅ | cascade confirmed in DB: folder + task + **status** `deleted_at` all set — `ExecuteUpdateAsync` for statuses was missing and added; DB shows 28 deleted statuses vs 8 live |
| Space Delete — other client receives Space D (online, live Delta) | ✅ | confirmed: other client's stores cleared immediately via `remove()` cascade; `devLog` bug (ReferenceError crashing cascade silently) found and fixed during this test |
| Space Delete — other client receives Space D (offline → reconnect, DeltaBatch) | ✅ | DeltaBatch catch-up delivers Space D; cascade removes from store + IDB; parent-space guard blocks any in-flight echo from re-inserting child entities |
| Space Delete — in-flight child tx race (another client deletes space while your task update is mid-HTTP) | ✅ | server returns 400 for update on deleted-space task; parent-space guard in `applyDelta` prevents echo from re-adding task to store; `cancelByEntityId` now covers in-flight (not just pending) txs |
| Space Create / Update / Delete (offline → flush) | 🔶 | same mutation path as Task offline, not yet deliberately tested end-to-end |

## Per-entity CRUD (Folder)
| Scenario | Status | Notes |
|---|---|---|
| Folder Create (online) | ✅ | tested via `/dev/sync-test` |
| Folder Update (online) | ✅ | tested via `/dev/sync-test` |
| Folder Delete — initiator (online) | ✅ | tasks reparented (`folderId = null`) in store+DB immediately; backend emits Task U events + Folder D as batch |
| Folder Delete — other client receives Task U + Folder D | 🔶 | Task U deltas reparent tasks via normal upsert path; Folder D removes folder from store+DB; `applyDelta` D case cancels any pending Folder ops — built, not tested from a second client |
| Folder Create / Update / Delete (offline → flush) | 🔶 | not yet deliberately tested |

## Per-entity CRUD (everything else)
| Entity | Status | Notes |
|---|---|---|
| Comment (Create/Update/Delete) | 🔶 | backend slices at `/api/comments/sync`; `delta-handler.ts` `Comment` case added; `frontend/src/mutations/comment.mutations.ts` built (full optimistic+queue+immediate-send pattern, matches Task). `BatchFlushHandler` covers it too. Not exercised via any real UI yet — no component calls `CommentMutations` |
| Document (Update/Delete) | 🔶 | backend slices at `/api/documents/sync/{id}`; no standalone Create (side-effect only); `document.mutations.ts` built (Update/Delete only). Not exercised via any real UI yet |
| DocumentBlock (Create/Update/Delete) | 🔶 | backend slices at `/api/document-blocks/sync`, single-entity; `delta-handler.ts` already had the `DocumentBlock` case; `document-block.mutations.ts` built. Not exercised via any real UI yet |
| Assignee (Create/Delete) | 🔶 | backend slices at `/api/assignees/sync`; `AssigneeStore`/`AssigneeDB` built from scratch + wired into `RootStore` (DB_VERSION bumped to 3); `delta-handler.ts` `Assignee` case added; `assignee.mutations.ts` built (Create/Delete only, no Update). Not exercised via any real UI yet |
| Workspace Update | 🔶 fixed | was a **live bug**: `UpdateWorkspaceHandler` never emitted a `SyncEvent`, so the frontend's already-built optimistic+queued `update()` never got its transaction dequeued (stuck forever). Now emits SyncEvent + broadcast like Task/Space. Not yet re-tested via the real app (no test page coverage for Workspace) |
| Workspace Delete | 🔶 | `DeleteWorkspaceCommand` built from scratch — no reachable endpoint existed before (frontend's `delete()` was hitting a 404). Backend-first, no queue, matches `WorkspaceMutations.delete()`. Not tested |
| Workspace Create | 🔶 | added idempotency (trace ID was already sent by the frontend but ignored server-side); behavior otherwise unchanged (backend-first, no SyncEvent needed since not queued) |
| Status (batch Create/Update/Delete rows) | 🔶 | ported off legacy (was "N/A by design" — reversed per 2026-07-01 decision to treat Status as a batch-shaped sync entity); `PUT /api/statuses/sync/batch`; soft-delete instead of legacy hard-delete; frontend `status.mutations.ts` is still an empty stub |
| Member (batch Update role/status, batch Remove) | 🔶 | ported off legacy, no Create slice (stays email-invite via legacy `POST /workspaces/{id}/members`); `PUT /api/members/sync/batch`, `POST /api/members/sync/remove`; frontend `member.mutations.ts` is still an empty stub |
| EntityAccess (batch Create/Update/Delete rows) | 🔶 | ported off legacy; `PUT /api/entity-access/sync/batch`; soft-delete via `entity.Remove()` instead of legacy's hard `db.EntityAccesses.Remove` (which was bypassing the entity's own `Remove()`/`SoftDelete()` method); no `entity-access.mutations.ts` exists on the frontend yet |
| Subtask | N/A — already covered | a subtask is just a `ProjectTask` row with `ParentTaskId` set; `CreateTaskCommand`/`UpdateTaskCommand` already support it. Legacy `CreateSubTask`/`UpdateSubTask`/`DeleteSubTask` left untouched, not ported |
| Assignee (Create/Delete) | 🔶 | built `Api/Features/AssigneeFeatures/{CreateAssignee,DeleteAssignee}`, single-entity (not legacy's diff/changeset `UpdateTaskAssigneesCommand`); `POST /api/assignees/sync`, `DELETE /api/assignees/sync/{id}`; no frontend mutation logic or delta-handler case yet (`SyncEntityType.Assignee` is new) |
| Favorite (Toggle/Reorder) | 🔶 | built `Api/Features/FavoriteFeatures/{ToggleFavorite,ReorderFavorite}`, deliberately backend-first with **no SyncEvent/broadcast** (personal per-member data, broadcasting would leak favorites to the whole workspace); `POST /api/favorites/toggle`, `PUT /api/favorites/reorder`; no `GetFavorites`-equivalent read endpoint built; no frontend mutation logic yet |
| Workspace list fetch | 🔶 | built `Api/Features/WorkspaceFeatures/FetchWorkspaces` (`GET /api/workspaces/sync`) — ported off legacy `GetWorkspaceListHandler` almost directly (same Dapper SQL, cursor pagination, HybridCache). This one **is** a Get query despite the general "no Get queries" pattern elsewhere in this migration, because Workspace is a read-replica entity that bypasses Bootstrap/Delta entirely — there's no other way for the client to learn its workspace list. Not tested; no frontend caller wired to it yet |
| Workspace join by code | 🔶 | built `Api/Features/WorkspaceFeatures/JoinWorkspaceByCode` (`POST /api/workspaces/sync/join`) — ported off legacy `JoinWorkspaceByCodeHandler`, **fixed a real bug in the process**: the legacy existing-member lookup omitted `ProjectWorkspaceId`, so it could match the caller's membership row in an unrelated workspace and reactivate/rejoin that one instead of the one being joined by code. No `SyncEvent` (Workspace doesn't participate in that stream); keeps the legacy personal `NotifyUserAsync(..., "WorkspaceJoined", ...)` push. Not tested; no frontend caller wired to it yet |
| Workspace list — IndexedDB persistence, recheck-on-reconnect | ⬜ | `FetchWorkspacesQuery` returns data but nothing writes it to `workspaceDB` yet (`switchWorkspace()` has no offline data for workspaces not otherwise cached); no trigger exists to refetch the list when the app comes back online to catch "added to a workspace while offline" — deferred, not yet designed |
| Notifications (Fetch/MarkRead) | 🔶 | built `Api/Features/NotificationFeatures/{FetchNotifications,MarkNotificationsRead}` — same read-replica treatment as Workspace (Get query genuinely needed, no SyncEvent/broadcast). Routes: `GET /api/notifications/sync`, `PUT /api/notifications/sync/read`. Not tested; no frontend caller wired to it yet (notificationStore/notificationDB already exist per root.store.ts) |

## Batch flush (`POST /api/sync/batch`)
| Scenario | Status | Notes |
|---|---|---|
| Flush N queued transactions in one HTTP call | 🔶 | **both sides now fully built** — frontend `TransactionQueue.flush()`/`SyncEngine.sendBatch()` (already existed, found built mid-session) posts `{items:[{traceId,entityType,action,entityId,data}]}`; backend `BatchFlushHandler` originally only handled Task/Space/Folder — **extended this session** to also cover Comment (C/U/D), Document (U/D), DocumentBlock (C/U/D), Assignee (C/D), Workspace (U). Before this fix, any queued mutation for those entity types would flush, get `{success:false}` forever, and retry indefinitely without ever succeeding. Not yet manually tested end-to-end |
| Partial batch (some items fail, others succeed) | 🔶 | handler continues on per-item failure (try/catch per item), returns per-item results; client dequeues only successes (failures go back to `pending` via `markPending`) — built, not tested |
| Retry full batch after mid-batch connection drop | 🔶 | safe because each item has its own idempotency check — resending already-processed traceIds is a no-op — built, not tested |
| Status/Member/EntityAccess through the queue | N/A | these never go through `TransactionQueue`/batch-flush at all — they're already-batch-shaped endpoints (`PUT /api/statuses/sync/batch` etc.) called directly, not per-item queued transactions |

## Bootstrap
| Scenario | Status | Notes |
|---|---|---|
| Bootstrap includes Spaces/Folders/Statuses (not just Tasks) | ✅ | `GetBootstrapHandler` returns all four entity types; tested cold-start + force re-bootstrap via `/dev/sync-test`. Bootstrap SQL correctly filters deleted spaces via `INNER JOIN project_spaces s ON s.id = ... AND s.deleted_at IS NULL` |
| Force re-bootstrap clears stale IDB state | ✅ | `forceBootstrap()` now clears all four entity DBs before `putMany` — prevents accumulated stale records from old delta events inflating store counts (was causing 24 statuses when 8 expected) |
| Bootstrap stale statuses from deleted spaces | ✅ fixed | Root cause: `DeleteSpaceHandler` never cascade-deleted statuses — fixed. Secondary fix: `forceBootstrap` now clears before re-hydrating |
| Bootstrap fetch priority + batching | ⬜ not implemented | `GetBootstrapHandler` runs 4 Dapper queries (Tasks, Spaces, Folders, Statuses) sequentially on one connection and returns them as a single combined response — no priority order, no streaming. For a large workspace, the small nav-critical data (Spaces/Folders/Statuses) is stuck waiting behind the potentially-huge Tasks query, and the frontend can't render anything until all four arrive together. Future direction: split into priority-ordered phases (structural data first — apply Spaces/Folders/Statuses and let the UI render nav immediately — then fetch/stream Tasks separately), and/or batch the structural queries into one combined SQL statement instead of 3 round-trips. |

## Connection lifecycle
| Scenario | Status | Notes |
|---|---|---|
| First join workspace (cold start, no local data) | ✅ | bootstrap path confirmed — all four entity types returned, hydrated into stores + IDB, `lastSyncId` written to `__metadata` |
| Returning to workspace with existing local data (lastSyncId > 0) | ✅ | skips bootstrap, goes straight to `connect()` → `SyncHub.OnConnectedAsync` sends `DeltaBatch` catch-up with 0 events (up to date) |
| Reconnect after dropped connection (real network offline) | ✅ | `withAutomaticReconnect({ nextRetryDelayInMilliseconds: () => 5000 })` retries forever (was default 4 retries then permanent disconnect — fixed); confirmed reconnect at attempt 8 in tests; `DeltaBatch` catch-up delivered and processed on reconnect |
| Offline (flag) → online (flag) transition with queued mutations | ✅ | queue flushed manually via "Flush Queue" button; mutations send correctly |
| Real network offline → online with pending queue + missed deltas | ✅ | `onreconnected` flushes queue; `OnConnectedAsync` sends DeltaBatch catch-up; both paths tested including Space D cascade on reconnect |
| Multiple browser tabs same workspace (cross-tab delta broadcast) | ✅ | tested with 4 tabs; each receives the same live Delta for space delete; each processes cascade independently; 4× `[SyncEngine] Delta received: Space D` logs confirmed |
| SignalR reconnect with stale/large backlog (many missed events) | 🔶 | `GetChangesAsync` has no pagination/limit — confirmed working for 12–23 events in testing, untested at volume |

## Conflict / correctness edge cases
| Scenario | Status | Notes |
|---|---|---|
| Two offline edits to the same task before reconnect (U+U squash) | 🔶 | `TransactionQueue.squash()` merges multiple U txs per entity (last-write-wins) — built, not yet deliberately tested end-to-end |
| Create+Delete of the same never-synced entity while offline (C+D cancel) | 🔶 | squash rule 1: both cancelled, zero network calls — built, not yet deliberately tested |
| D beats pending U (entity updated then deleted offline) | 🔶 | squash rule 2: U cancelled, only D sent — built, not yet deliberately tested |
| C+U(s) merge into one C while offline | 🔶 | squash rule 3: merged into final C — built, not yet deliberately tested |
| Another user deletes an entity you had pending ops for (queue jam prevention) | 🔶 | `applyDelta` D case calls `cancelByEntityId(delta.entityId)` before removing entity — built, not yet deliberately tested from two clients |
| Another user deletes a space while you had pending ops on its children | 🔶 | Space D `dbDelete` calls `cancelByEntityId` for all child folder/task IDs — built, not yet deliberately tested |
| Duplicate send of same transaction (idempotency) | 🔶 | `IdempotencyService` + `processed_traces` table built for this, not deliberately tested |
| Out-of-order delta application (DeltaBatch arrives before an in-flight Delta) | ⬜ | not tested |

## Auth / security
| Scenario | Status | Notes |
|---|---|---|
| `SyncHub` connection without valid auth | ⬜ | no `[Authorize]` on hub at all currently — flagged as pre-production gap in BACKEND_SYNC_CONTEXT.md §10 |
| Cross-workspace leakage (user in workspace A receives workspace B's deltas) | ⬜ | relies entirely on SignalR group correctness, never adversarially tested |

## App integration
| Scenario | Status | Notes |
|---|---|---|
| Sync engine wired into real app (not just `/dev/sync-test`) | 🔶 | `SyncProvider` (`frontend/src/sync/sync-provider.tsx`) now mounts `RootStore`+`SyncEngine` per-workspace in `$workspaceId.tsx`, alongside the existing Redux `WorkspaceProvider`. Exposes `useSyncEngine()`/`useSyncReady()`. Not yet tested live by anyone but Duc manually |
| Task detail view (`views/task`) on the new sync system | 🔶 | `TaskView`, `TaskDetailCanvas`, `TaskSubtasks` fully moved off Redux — read/write via `rootStore.taskStore`/`TaskMutations`. `TaskAssignees`/`TaskComments` are a **hybrid**: still use the legacy RTK fetch for initial load (no fetch endpoint exists for Assignee/Comment on the new backend — Assignee isn't in Bootstrap, Comment is deliberately per-task-paginated instead), but bridge fetched results into `assigneeStore`/`commentStore` and route all mutations through `AssigneeMutations`/`CommentMutations` so live Deltas from other users land in the same place the component reads. Space/Folder/Member/EntityAccess/BlockEditor inside the task view are untouched (still Redux/legacy) |
| Debounced field-edit architecture | ✅ fixed | Real bug found+fixed twice in this pass: (1) first version bundled the optimistic UI update inside the debounced network send, so the UI waited the full debounce delay to show any change; (2) second version hand-rolled patch-merging in the component, duplicating `TransactionQueue.squash()`'s job. Final design: `TaskMutations`/`SpaceMutations`/`FolderMutations` all split into `updateLocal()` (store+IndexedDB+enqueue, instant, no network) and `update()` (calls `updateLocal()` + immediate send, for single deliberate actions). Rapid-edit components (`TaskDetailCanvas`, `TaskSubtasks`) call `updateLocal()` on every change and debounce a shared `useDebouncedFlush()` → `syncEngine.flushQueue()` trigger — `squash()` merges N pending edits into one network call for free |
| Coexistence with old Redux/RTK Query system during migration | 🔶 | proven pattern now exists (see Task detail view row above) — bridge-and-mutate-through-new-system for entities without a new fetch endpoint, full-swap for entities that don't need one (Task) |

---
**How to use this:** when you finish building+testing a scenario, flip its status and add a one-line note on how it was verified (manual test page, direct DB query, etc.) so the next person doesn't have to take it on faith.
