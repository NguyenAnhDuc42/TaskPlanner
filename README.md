# TaskPlanner

TaskPlanner is a collaborative task management platform inspired by tools like ClickUp and Notion. It began as a personal solution for organizing development work and evolved into a full-stack application exploring real-time collaboration, hierarchical workspaces, and scalable backend architecture

## Technical Highlights
- Vertical Slice Architecture with CQRS feature organization
- Hierarchical workspace model (Workspace → Space → Task → SubTask, Folder as a sidebar-only organizer)
- Role-based authorization with entity-level permissions
- Offline-first sync engine: MobX + IndexedDB local store, reconciled via SignalR Delta/DeltaBatch against a sequence-numbered event log
- JWT + OAuth authentication

<img width="1598" height="721" alt="Image" src="https://github.com/user-attachments/assets/c986c18a-07be-40f0-91a2-0d888c1289a4" />

<img width="1600" height="726" alt="Image" src="https://github.com/user-attachments/assets/31d8958e-2ba7-4434-9519-4d67aadf1e1b" />

<img width="1600" height="730" alt="Image" src="https://github.com/user-attachments/assets/223a305f-b07a-4d24-8cb0-eafa014b7b2f" />

<img width="1600" height="721" alt="Image" src="https://github.com/user-attachments/assets/ebc94090-d809-48f8-a465-31097e38c767" />


## Features

**Workspace & Hierarchy**
- Workspace → Space → Task → SubTask. Folder still exists as a plain sidebar organizer but was
  demoted out of boards/filters/task cards — it wasn't earning its place as a first-class layer
- Drag-and-drop reordering and cross-space item movement
- Role-based access control (Owner, Admin, Member, Guest) with entity-level privacy settings;
  Guests get clean read-only 403s instead of being bounced through a false "session expired" retry

**Collaboration**
- Real-time updates via SignalR — changes broadcast instantly to all connected users
- Invite members by email or join code
- Notification system with real-time delivery

**Tasks**
- Subtasks, assignees, priority, status, due dates
- Batch operations (update status/priority/dates across multiple tasks)

**Content**
- Block-based document editor per task and space
- Rich text with headings, lists, checkboxes, and inline formatting
- Per-entity activity feed showing who did what, when

**Workspace**
- Flat, single-page settings (name/icon, danger zone) — no fake tabs for features that don't exist
- Cross-space "My Tasks" list — every task assigned to you, grouped by space

**Auth**
- JWT authentication with refresh token rotation
- OAuth via Google and GitHub
- Secure HttpOnly cookie session management

**Infrastructure**
- Background job processing with Hangfire
- Rate limiting per user

---

## Tech Stack

**Backend:** .NET 10, PostgreSQL, Entity Framework Core, SignalR, Hangfire, JWT Auth, OAuth (Google + GitHub)

**Frontend:** React, TypeScript, MobX + IndexedDB (offline-first sync store), TanStack Router, Vite, Tailwind CSS

**Infrastructure:** Railway (API + PostgreSQL), Vercel (Frontend), Docker

---

## Architecture

The backend follows Vertical Slice Architecture, organizing each feature into isolated command/query handlers, validators, and endpoints. This keeps business logic localized and reduces coupling between features while remaining simple enough for a solo project.

The frontend runs a local-first sync engine: optimistic writes land in MobX stores and IndexedDB immediately, get queued in a `TransactionQueue` (squashed before sending — multiple offline edits to the same entity collapse into one network call), and reconcile against the server's append-only `sync_events` log via SignalR `Delta`/`DeltaBatch` messages. A cold start or stale session pulls a full `Bootstrap` snapshot; a reconnect pulls only what changed since the last known sequence number, collapsed to one event per entity so a long-offline client doesn't replay every intermediate edit. Redux/RTK Query has been fully removed — Task, Space, Folder, Status, Member, Assignee, Favorite, Notification, Workspace, and Auth all run on MobX + IndexedDB now.

---

## What I learned the hard way

**Scope creep kills timelines.** The feature set doesn't justify the development time. I kept re-architecting instead of finishing: rebuilt the entire data layer more than once, stood up a caching layer and then tore it back out once it caused more bugs than it solved, half-built features and abandoned them, migrated the same subsystem off Redux in pieces over multiple passes instead of committing to one clean cut. Every rewrite taught me something real, but the sum of all of them is a project that took far longer to reach "done" than the feature set alone would ever justify.

**Real-time is hard to get right.** The SignalR implementation works but isn't reliable. SSE fallback through Vercel's proxy generates excessive requests. WebSocket connections drop and the reconnect logic creates request storms. The lesson: don't bolt real-time onto a request-based system — design for it from the start.

**Feature creep, not feature count, is what kills a solo project.** Too much time went into planning half-formed ideas before they were real, which meant parts of the app got redesigned too many times chasing a "final" version instead of committing to one and moving on. The fix was ruthless cutting: anything that wasn't fully wired up got removed rather than left half-built, so the app is smaller now but everything in it actually works.

**Two data sources fight each other.** The frontend used to maintain both a Redux entity store and an RTK Query cache simultaneously. They conflicted, causing redundant fetches and UI inconsistencies. The fix — since shipped, not just diagnosed — was giving ownership to one layer and making everything else derived from it: MobX stores are the reactive read layer, IndexedDB is the persistent layer beneath them, and the server is a sync target reconciled via a sequence-numbered event log, not a second cache fighting for the same data. A true single source is still impossible given IndexedDB's async nature — the goal was a clear ownership hierarchy, not literal consolidation, and that's what's running now for the entities that have been migrated.

**Ship first, iterate after.** The biggest mistake was trying to get everything right before shipping. The correct approach: define a scope, finish it, deploy it, then improve from real usage.

---

## Roadmap

### v0.1.0 — Initial Deploy `2026-06-26`
- Deployed to Railway (API + PostgreSQL) and Vercel (Frontend)
- Full auth flow working — JWT, refresh tokens, Google + GitHub OAuth
- Core hierarchy (Workspace → Space → Folder → Task) live
- Real-time collaboration via SignalR (SSE fallback)
- Document editor per task and space
- Notification system with real-time delivery
- Invite by email and join by code

### v0.2.0 — Sync Engine `2026-07-03`
- Sequence-numbered `sync_events` log + `Bootstrap`/`Delta`/`DeltaBatch` replacing reconnect-and-refetch
- Frontend data layer rebuilt: MobX stores + IndexedDB as the local-first source of truth, `TransactionQueue` (with squash — offline edits to the same entity collapse to one send) reconciling against the server
- Migrated off Redux: Task, Space, Folder, Status, Member, Assignee, Favorite, Notification, Comment — full CRUD, offline queueing, and live multi-client reconnect catch-up
- Reconnect catch-up now collapses to the latest event per entity instead of replaying full history — a long-offline client no longer replays every intermediate edit to something that's since changed again (or been deleted)
- Bootstrap's 8 sequential queries consolidated into a single round-trip
- Stale-session detection (`databaseVersion`) — existing sessions force a fresh Bootstrap when its shape changes, instead of silently missing new fields/entities forever
- Sidebar drag-and-drop fixes (collision detection, reorder-target tracking) and expand/collapse state persistence

### v0.3.0 — Redux Removal & Custom Domain `2026-07-05`
- Migrated the remaining Redux/RTK Query surface (Auth, user preferences, workspace list/detail) to MobX/IndexedDB — Redux fully removed from the frontend
- Custom domain live (`orpus.app` / `api.orpus.app`) — cookies, SignalR, and OAuth all run same-parent-domain now; the cross-domain JWT-query-string workaround was ripped out
- `SyncHub`/`WorkspaceHub` authentication — both hubs now validate the caller and workspace membership before joining groups (previously unguarded)
- Local-first resilience: cached IndexedDB data now renders immediately regardless of SignalR connection state; a failed/slow reconnect (e.g. resuming an AFK tab) retries in the background instead of blocking the UI with a false "failed to load"

### v0.4.0 — Simplification Pass `2026-07-12`
- Fixed a permission bug that blocked Guests from viewing tasks they had read access to
- Cut several half-built surfaces (a placeholder dashboard, an empty settings page) in favor of
  a smaller set of pages that are fully functional
- Simplified the hierarchy — Folder is now a lightweight sidebar organizer instead of a full
  filterable layer, matching how it's actually used
- Added a per-entity activity feed and a cross-space "My Tasks" view

### Planned — v0.5.0
- Rename "Status" — it's really a workspace-wide board column/stage now, not a per-item status
  field, and the name stopped matching what it models once it became shared across spaces
- Field-level activity diffs ("moved from Todo to In Progress") instead of action-level only
- Batch flush (`POST /api/sync/batch`) end-to-end testing at scale, plus offline conflict-edge-case coverage (squash rules, out-of-order delta application)
- Bootstrap parallel query batching (structural data vs. secondary data on separate connections) — deferred pending real latency numbers, not yet worth the added complexity
- Document block editor rewrite on Yjs (CRDT-based collaborative editing) — separate initiative, not started
