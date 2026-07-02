# TaskPlanner — Claude Code Context

## Read these first (always)
- `FRONTEND_SYNC_CONTEXT.md` — frontend sync architecture, bugs found, queue/delta/IDB patterns
- `BACKEND_SYNC_CONTEXT.md` — backend handlers, cascade delete, permission patterns, bootstrap
- `SYNC_SCENARIOS.md` — living checklist of what's built/tested vs not (check before assuming anything works)

## Stack
- **Backend:** ASP.NET Core, EF Core, Dapper, SignalR, PostgreSQL
- **Frontend:** React, MobX, IndexedDB (idb), SignalR JS client, TypeScript
- **Pattern:** hybrid local-first sync — optimistic UI + TransactionQueue + SignalR Delta/DeltaBatch

## What this project is
A task planner app with an offline-first sync engine being built alongside (and eventually replacing) the old Redux/RTK Query system.

## Key paths
- `frontend/src/sync/` — sync engine, delta handler, transaction queue
- `frontend/src/db/` — IDB schemas and per-entity DB operations
- `frontend/src/stores/` — MobX stores (root.store.ts + per-entity)
- `frontend/src/mutations/` — task/space/folder/workspace mutations
- `server/Api/Features/` — backend handlers (one folder per feature/entity)
- `server/Api/Hubs/SyncHub.cs` — SignalR hub
- `/dev/sync-test` — manual test page for the sync engine (not wired into real app yet)

## Current state
Sync engine is proven for Task, Space, Folder CRUD (online + offline + cascade delete). Statuses exist in bootstrap/cascade but have no mutation handlers yet. The system only runs in the test page — not mounted in the real app.

## User
Duc — owns the full stack. Direct, prefers concise responses. Reads code, doesn't need things over-explained.
