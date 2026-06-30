# Sync Engine — Scenario Checklist

*Living tracker. Update the status the moment a scenario is built/tested, not after. This is the source of truth for "is X actually done" — don't trust memory or assume from code existing.*

**Companion docs:** `FRONTEND_SYNC_CONTEXT.md`, `BACKEND_SYNC_CONTEXT.md` (repo root).

Status legend: ✅ built + manually tested · 🔶 built, not tested · ⬜ not built

## Per-entity CRUD (Task)
| Scenario | Status | Notes |
|---|---|---|
| Create (online) | ✅ | tested via `/dev/sync-test` + Postgres check on `sync_events` |
| Create (offline → flush) | ✅ | queued in `__transactions`, flushed after reconnect |
| Update | ⬜ | client mutation exists (`TaskMutations.update`), no backend slice (`Api/Features/TaskFeatures/UpdateTask/` empty) |
| Delete | ⬜ | client mutation exists (`TaskMutations.delete`), no backend slice |

## Per-entity CRUD (everything else)
| Entity | Status | Notes |
|---|---|---|
| Space / Folder / Status / Comment / Document / DocumentBlock / Member / EntityAccess | ⬜ | DB/store scaffolding exists, no mutation logic, no backend slice |

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
| Two offline edits to the same task before reconnect (queue squashing) | ⬜ | known gap, documented in FRONTEND_SYNC_CONTEXT.md §8 — queue currently sends all transactions sequentially, no squashing |
| Server rejects a queued offline transaction on flush (e.g. entity deleted elsewhere meanwhile) | ⬜ | rollback path exists for *online* 4xx rejection, not verified for *deferred* flush rejection |
| Duplicate send of same transaction (idempotency) | 🔶 | `IdempotencyService` + `processed_traces` table built for this, not deliberately tested (e.g. force a retry and confirm no duplicate `sync_events` row) |
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
