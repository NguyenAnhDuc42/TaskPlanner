# TaskPlanner

TaskPlanner is a collaborative task management platform inspired by tools like ClickUp and Notion. It began as a personal solution for organizing development work and evolved into a full-stack application exploring real-time collaboration, hierarchical workspaces, and scalable backend architecture

## Technical Highlights
- Vertical Slice Architecture with CQRS feature organization
- Hierarchical workspace model (Workspace → Space → Folder → List → Task)
- Role-based authorization with entity-level permissions
- Optimistic UI with Redux entity store + SignalR synchronization
- JWT + OAuth authentication

<img width="1598" height="721" alt="Image" src="https://github.com/user-attachments/assets/c986c18a-07be-40f0-91a2-0d888c1289a4" />

<img width="1600" height="726" alt="Image" src="https://github.com/user-attachments/assets/31d8958e-2ba7-4434-9519-4d67aadf1e1b" />

<img width="1600" height="730" alt="Image" src="https://github.com/user-attachments/assets/223a305f-b07a-4d24-8cb0-eafa014b7b2f" />

<img width="1600" height="721" alt="Image" src="https://github.com/user-attachments/assets/ebc94090-d809-48f8-a465-31097e38c767" />


## Features

**Workspace & Hierarchy**
- 5-level hierarchy: Workspace → Space → Folder → List → Task
- Drag-and-drop reordering and cross-space item movement
- Role-based access control (Owner, Admin, Member) with entity-level privacy settings

**Collaboration**
- Real-time updates via SignalR — changes broadcast instantly to all connected users
- Invite members by email or join code
- Notification system with real-time delivery

**Tasks**
- Subtasks, assignees, priority, status, due dates
- Batch operations (update status/priority/dates across multiple tasks)
- Cursor-based pagination for large task lists

**Content**
- Block-based document editor per task and space
- Rich text with headings, lists, checkboxes, and inline formatting

**Auth**
- JWT authentication with refresh token rotation
- OAuth via Google and GitHub
- Secure HttpOnly cookie session management

**Infrastructure**
- Background job processing with Hangfire
- Hybrid in-memory + distributed caching
- Rate limiting per user

---

## Tech Stack

**Backend:** .NET 10, PostgreSQL, Entity Framework Core, SignalR, Hangfire, JWT Auth, OAuth (Google + GitHub)

**Frontend:** React, TypeScript, Redux Toolkit, RTK Query, TanStack Router, Vite, Tailwind CSS

**Infrastructure:** Railway (API + PostgreSQL), Vercel (Frontend), Docker

---

## Architecture

The backend follows Vertical Slice Architecture, organizing each feature into isolated command/query handlers, validators, and endpoints. This keeps business logic localized and reduces coupling between features while remaining simple enough for a solo project.

The frontend uses a Redux entity store for local state with RTK Query for server synchronization, sitting on top of a SignalR real-time layer.

---

## What I learned the hard way

**Scope creep kills timelines.** The feature set doesn't justify the development time. I kept learning new things and incorporating them mid-build — SignalR, CRDT concepts, cursor-based pagination, hybrid caching — without finishing what was already started.

**Real-time is hard to get right.** The SignalR implementation works but isn't reliable. SSE fallback through Vercel's proxy generates excessive requests. WebSocket connections drop and the reconnect logic creates request storms. The lesson: don't bolt real-time onto a request-based system — design for it from the start.

**Two data sources fight each other.** The frontend maintains both a Redux entity store and RTK Query cache simultaneously. They conflict, causing redundant fetches and UI inconsistencies. It works, but it's heavier than it should be. The right approach is a persistent local store (IndexedDB) as the source of truth, with a reactive in-memory layer on top — not two competing remote-cache systems. RTK Query and the entity store fight because neither truly owns the data. The fix is giving ownership to one layer and making the other derived from it. A true single source is impossible on the frontend due to IndexedDB's async nature — the goal is a clear ownership hierarchy, not literal consolidation.

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

### Planned — v0.2.0
- Custom domain — prerequisite for proper cookie security, OAuth, and CORS
- Sync engine: sequence-numbered event log + delta endpoint replacing reconnect-and-refetch
- Activity feed powered by the event log (same table, no extra work)
- Frontend data layer: IndexedDB as persistent store, reactive in-memory layer on top, server as sync target — clear ownership hierarchy replacing the current competing caches
- Modular backend architecture (Auth, Workspace, Hierarchy, Documents, Sync)
- WebSocket stability — remove SSE fallback dependency
