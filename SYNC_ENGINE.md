# Sync Engine Architecture

## Overview

Local-first sync architecture where the client owns entity identity (client-generated UUIDs) and the server's job is to persist and broadcast — not to generate or dictate IDs.

Scope: workspace-bound entities only (spaces, folders, tasks, documents, members). Workspace creation itself remains server-authoritative.

---

## Client-Side: IndexedDB Structure

### Entity Stores
One store per entity type — mirrors what is currently in Redux entity store.
```
spaces, folders, tasks, members, statuses, comments, document_blocks, ...
```

### `__metadata` (one row per workspace)
Tracks sync state for a workspace session.

| Field | Type | Description |
|---|---|---|
| `workspaceId` | string | Workspace this metadata belongs to |
| `firstSyncId` | number | The `lastSyncId` at time of full bootstrap. If server has pruned `sync_actions` older than this, a full re-bootstrap is required |
| `lastSyncId` | number | Most recent `sync_id` received and applied. Used for delta catch-up on reconnect |
| `databaseVersion` | number | Schema version. If server bumps this, client IndexedDB schema is outdated — trigger full re-bootstrap |

### `__transactions` (pending writes)
Tracks mutations sent to the server but not yet confirmed.

| Field | Type | Description |
|---|---|---|
| `id` | string | Client-generated UUID (same as entity ID) |
| `entityType` | string | `"task"`, `"folder"`, `"space"`, etc. |
| `action` | string | `"CREATE"`, `"UPDATE"`, `"DELETE"` |
| `payload` | json | Full payload sent to server |
| `createdAt` | timestamp | When the mutation was initiated |
| `retryCount` | number | How many times this has been retried |
| `status` | string | `"pending"` \| `"confirmed"` \| `"failed"` |

---

## Creation Flow (Happy Path)

```
STEP 1: User clicks "Create Task"
  → Generate UUID on client
  → Add entity to in-memory store (user sees it instantly)
  → Persist entity to IndexedDB entity store
  → Write to IndexedDB __transactions { status: "pending" }
  → Send HTTP POST to server with client UUID as entity ID

STEP 2a: Server SUCCESS
  → Server saves to tasks table using client-provided UUID
  → Server appends row to sync_actions table (gets sync_id)
  → Server broadcasts via SignalR:
      { syncId: 1006, action: "I", entityType: "task", entityId: "uuid", data: {...} }

  → Client receives SignalR broadcast
  → Client checks: entityId matches a pending __transaction → skip applying (already in store)
  → Client updates __metadata.lastSyncId = 1006
  → Client removes transaction from __transactions (confirmed)
  → Done — no flicker, no reconciliation
```

## Creation Flow (Failure Path)

```
STEP 2b: Server FAILS
  → Client receives error response
  → Client removes entity from in-memory store
  → Client removes entity from IndexedDB entity store
  → Client removes transaction from __transactions
  → Show error toast to user
  → Done
```

---

## Reconnect / Delta Catch-Up Flow

```
SignalR reconnects
  → Client reads __metadata.lastSyncId (e.g. 1005)
  → Client calls GET /workspaces/{id}/sync?since=1005
  → Server returns all sync_actions where sync_id > 1005
  → Client applies each action to IndexedDB + in-memory store
  → Client updates __metadata.lastSyncId to latest received
  → No full refetch, no cache invalidation storm
```

### Re-bootstrap Triggers
Full re-bootstrap required when:
- `firstSyncId` is older than server's oldest pruned `sync_id` (server returns 410 Gone on delta request)
- `databaseVersion` mismatch between client and server
- First visit to workspace (no `__metadata` row exists)

---

## Backend: `sync_actions` Table

Event log — append-only. Every mutation appends a row.

| Column | Type | Description |
|---|---|---|
| `id` | bigserial | Auto-incrementing sequence (this is the `sync_id`) |
| `workspace_id` | uuid | Workspace scope |
| `action` | char(1) | `I` = insert, `U` = update, `D` = delete |
| `entity_type` | varchar | `"task"`, `"folder"`, `"space"`, etc. |
| `entity_id` | uuid | The entity's UUID (client-generated on creates) |
| `data` | jsonb | Full entity snapshot at time of mutation |
| `actor_id` | uuid | Member who triggered the action |
| `created_at` | timestamptz | When the action was recorded |

### Pruning
Old entries can be pruned after a retention window (e.g. 30 days). When pruned, `databaseVersion` is bumped to force client re-bootstrap.

---

## Backend: Delta Endpoint

```
GET /workspaces/{id}/sync?since={syncId}

Response:
{
  "actions": [
    { "syncId": 1006, "action": "I", "entityType": "task", "entityId": "uuid", "data": {...} },
    { "syncId": 1007, "action": "U", "entityType": "folder", "entityId": "uuid", "data": {...} }
  ],
  "databaseVersion": 3,
  "latestSyncId": 1007
}

410 Gone → sync_id too old, client must full re-bootstrap
```

---

## SignalR Broadcast Payload

Every mutation broadcasts after writing to `sync_actions`:

```json
{
  "syncId": 1006,
  "action": "I",
  "entityType": "task",
  "entityId": "550e8400-e29b-41d4-a716-446655440000",
  "data": { ...full entity snapshot... }
}
```

Client deduplication rule: if `entityId` exists in `__transactions` with status `"pending"`, skip applying the broadcast (creator already has it). All other clients apply normally.

---

## Mutation Handler Pattern (Backend)

Every mutation handler follows this sequence inside a transaction:

```
1. Validate + authorize
2. Apply domain change to DB
3. Append to sync_actions (gets sync_id)
4. Commit transaction
5. Broadcast via SignalR (fire and forget)
```

Steps 1-4 are atomic. Step 5 is best-effort — if SignalR fails, clients catch up via delta on next reconnect.

---

## Bootstrap Flow (First Visit)

```
User opens workspace for first time (no __metadata row)
  → Call GET /workspaces/{id}/bootstrap
  → Server returns: all current entities + current sync_id + databaseVersion
  → Client persists everything to IndexedDB
  → Client writes __metadata { firstSyncId: X, lastSyncId: X, databaseVersion: Y }
  → Render from IndexedDB
  → Connect SignalR, start receiving live updates
```

---

## Implementation Order

1. **Backend: `sync_actions` table + migration**
2. **Backend: write to `sync_actions` in mutation handlers**
3. **Backend: delta endpoint + bootstrap endpoint**
4. **Backend: include `syncId` in SignalR broadcast**
5. **Frontend: IndexedDB setup + `__metadata` + `__transactions` stores**
6. **Frontend: bootstrap flow on first workspace visit**
7. **Frontend: apply SignalR broadcasts to IndexedDB + deduplicate own writes**
8. **Frontend: delta catch-up on reconnect**
9. **Frontend: client-generated UUIDs on create mutations**
10. **Frontend: rollback on failure**
