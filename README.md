# TaskPlanner

A personal task management application built to solve a real problem — I struggle with maintaining a consistent development schedule and keeping goals stable, so I built a tool to plan and track my own work.

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
- Optimistic UI with real-time reconciliation

---

## Tech Stack

**Backend:** .NET 10, PostgreSQL, Entity Framework Core, SignalR, Hangfire, JWT Auth, OAuth (Google + GitHub)

**Frontend:** React, TypeScript, Redux Toolkit, RTK Query, TanStack Router, Vite, Tailwind CSS

**Infrastructure:** Railway (API + PostgreSQL), Vercel (Frontend), Docker

---

## Architecture

The backend follows **vertical slice architecture** — each feature is self-contained with its own command, handler, and validator. This reflects how I naturally think about problems: by feature, not by layer. There's enough abstraction to stay maintainable without over-engineering.

The frontend uses a Redux entity store for local state with RTK Query for server synchronization, sitting on top of a SignalR real-time layer.

---

## What I learned the hard way

**Scope creep kills timelines.** The feature set doesn't justify the development time. I kept learning new things and incorporating them mid-build — SignalR, CRDT concepts, cursor-based pagination, hybrid caching — without finishing what was already started.

**Real-time is hard to get right.** The SignalR implementation works but isn't reliable. SSE fallback through Vercel's proxy generates excessive requests. WebSocket connections drop and the reconnect logic creates request storms. The lesson: don't bolt real-time onto a request-based system — design for it from the start.

**Two data sources fight each other.** The frontend maintains both a Redux entity store and RTK Query cache simultaneously. They conflict, causing redundant fetches and UI inconsistencies. It works, but it's heavier than it should be. The right approach is a single local-first store with the server as the sync target, not the source of truth.

**Ship first, iterate after.** The biggest mistake was trying to get everything right before shipping. The correct approach: define a scope, finish it, deploy it, then improve from real usage.

---

## Patch Notes

### v0.1.0 — Initial Deploy `2026-06-26`
- Deployed to Railway (API + PostgreSQL) and Vercel (Frontend)
- Full auth flow working — JWT, refresh tokens, Google + GitHub OAuth
- Core hierarchy (Workspace → Space → Folder → Task) live
- Real-time collaboration via SignalR (SSE fallback)
- Document editor per task and space
- Notification system with real-time delivery
- Invite by email and join by code

### Planned — v0.2.0
- Sync engine: sequence-numbered event log + delta endpoint
- Fix dual data source — migrate to single local-first store
- Activity feed powered by event log
- Modular backend architecture (Auth, Workspace, Hierarchy, Documents, Sync)
- WebSocket stability improvements
