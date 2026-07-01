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
| Space Delete — initiator (online) | ✅ | cascade confirmed in DB: folder + task `deleted_at` timestamps match space's to the millisecond — bulk `ExecuteUpdateAsync` fired atomically |
| Space Delete — other client receives Space D (online) | ✅ | confirmed: other client's stores cleared, DB cascade confirmed with matching timestamps |
| Space Delete — cascade pending child txs cancelled | 🔶 | `cancelByEntityId` called for all child IDs in both `SpaceMutations.delete()` and `delta-handler` Space D `dbDelete` — built, not deliberately tested offline |
| Space Create / Update / Delete (offline → flush) | 🔶 | same mutation path as Task offline, not yet deliberately tested |

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
| Status / Member | N/A by design | legacy handlers are batch + server-validated, not single-entity optimistic creates (Status only exists via `UpdateSpaceStatusesCommand`'s row-action list; Member only exists via email lookup against real Users) — staying on legacy endpoints, not being ported to the offline-first sync system |
| Comment / DocumentBlock | ⬜ | DB/store scaffolding exists, no mutation logic, no backend slice |

## Batch flush (`POST /api/sync/batch`)
| Scenario | Status | Notes |
|---|---|---|
| Flush N queued transactions in one HTTP call | ⬜ | backend `BatchFlushHandler` + frontend `sendBatch()` not built — see `BACKEND_SYNC_CONTEXT.md §11` for full design |
| Partial batch (some items fail, others succeed) | ⬜ | handler must continue on per-item failure, return per-item results; client dequeues only successes |
| Retry full batch after mid-batch connection drop | ⬜ | safe because each item has its own idempotency check — resending already-processed traceIds is a no-op |

## Bootstrap
| Scenario | Status | Notes |
|---|---|---|
| Bootstrap includes Spaces/Folders/Statuses (not just Tasks) | 🔶 | `GetBootstrapHandler`/`SyncEngine.bootstrap()` extended, builds clean, not yet manually tested. Same visibility rule (private-space + entity_access) applied to all four queries |
| Bootstrap fetch priority + batching | ⬜ not implemented | `GetBootstrapHandler` runs 4 Dapper queries (Tasks, Spaces, Folders, Statuses) sequentially on one connection and returns them as a single combined response — no priority order, no streaming. For a large workspace, the small nav-critical data (Spaces/Folders/Statuses) is stuck waiting behind the potentially-huge Tasks query, and the frontend can't render anything until all four arrive together. Future direction: split into priority-ordered phases (structural data first — apply Spaces/Folders/Statuses and let the UI render nav immediately — then fetch/stream Tasks separately), and/or batch the structural queries into one combined SQL statement instead of 3 round-trips. This is the bootstrap-side analog of the offline-queue "Queue Squashing Rules" in `FRONTEND_SYNC_CONTEXT.md` §9 — same instinct (don't treat a pile of heterogeneous data as one undifferentiated blob), different end of the pipe (initial fetch vs. offline send) |

## Connection lifecycle
| Scenario | Status | Notes |
|---|---|---|
| First join workspace (cold start, no local data) | 🔶 | `bootstrap()` path exists (`GetBootstrapHandler`), only returns Tasks — not manually verified end-to-end as a true first-time user |
| Returning to workspace with existing local data (lastSyncId > 0) | 🔶 | skips bootstrap, goes straight to `connect()` → `SyncHub.OnConnectedAsync` sends `DeltaBatch` catch-up — exercised incidentally during testing but not as its own deliberate scenario |
| Reconnect after dropped connection (still same session) | ✅ | simulated via DevTools throttle, confirmed `DeltaBatch received: N events` client log + dequeue |
| Offline → online transition (queued mutations exist) | ✅ | confirmed via offline-create + flush test |
| Multiple browser tabs / multiple devices same workspace | ⬜ | not tested — broadcast excludes sender connection via `X-Connection-Id`, but cross-tab/cross-device receive path never verified |
| SignalR reconnect with stale/large backlog (many missed events) | ⬜ | `GetChangesAsync` has no pagination/limit — untested at volume |

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
| Sync engine wired into real app (not just `/dev/sync-test`) | ⬜ | `RootStoreProvider` exists but isn't mounted anywhere real yet |
| Coexistence with old Redux/RTK Query system during migration | ⬜ | unstarted — old system untouched per original plan, no integration tested |

---
**How to use this:** when you finish building+testing a scenario, flip its status and add a one-line note on how it was verified (manual test page, direct DB query, etc.) so the next person doesn't have to take it on faith.
