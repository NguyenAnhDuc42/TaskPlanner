# Frontend Sync Architecture & Context

*Use this document as context for future chats to quickly spin up AI assistants on the current state of the frontend architecture.*

## 1. Core Architecture Pattern: Hybrid Local-First
The frontend uses a hybrid approach to balance zero-latency UI (optimistic updates) with strict backend authority.

- **Optimistic Entities (Tasks, Folders, Spaces, Comments):**
  These use the **Sync Engine**. When a user mutates these, the UI updates instantly (`RootStore` -> `IndexedDB`). A transaction is queued in the `TransactionQueue` and a synchronous API call is fired.
  - If the API succeeds: The transaction waits in the queue until SignalR confirms it via a broadcast Delta.
  - If the API fails (Network Error): The transaction sits in the queue to be retried on reconnect.
  - If the API fails (400/403 Server Rejection): The UI rolls back the change immediately and dequeues the transaction.

- **Read-Replica Entities (Workspaces, Notifications):**
  These **bypass** the Sync Engine because their mutations require strict backend logic (e.g., joining a workspace). 
  - They use standard `api.post` calls with loading spinners.
  - Upon success, the frontend manually updates the MobX store and IndexedDB cache.

## 2. Storage Layers
- **IndexedDB (Persistence):** Uses `idb`. There are two databases:
  1. `UserDB` (`user-schema.ts`): Stores global entities (`workspaces`, `notifications`). Opens on `initUser()`.
  2. `TaskPlanDB` (`schema.ts`): Stores workspace-specific entities (`tasks`, `spaces`, `folders`, `comments`, `document_blocks`, `entity_access`, `members`). Opens on `switchWorkspace()`.

- **MobX (Memory/Reactivity):** Centered around `RootStore`.
  - When `switchWorkspace()` is called, all workspace-specific stores are cleared and hydrated in parallel from IndexedDB.
  - Contains all discrete stores (e.g., `TaskStore`, `WorkspaceStore`).

## 3. The Backend Expectations
To support this frontend, the backend must be refactored to support:
1. **Client-Generated GUIDs**: The frontend creates offline, so it dictates the ID.
2. **Idempotency**: The frontend will retry if it drops connection. The backend must check the `X-Client-Trace-Id` header (or existing IDs) and return `200 OK` for duplicates.
3. **Partial Updates**: Backend must accept partial JSON (to prevent nulls from wiping out other fields).
4. **Sync Event Log**: Every mutation must write to a `SyncEvents` table in the DB.
5. **SignalR Broadcasts**: SignalR broadcasts rows from the `SyncEvents` table. The frontend `delta-handler.ts` checks the `ClientTraceId` to ignore its own changes, and merges foreign changes into IndexedDB/MobX.

## 4. Unsolved Edge Cases / Future Work
- **Transaction Squashing (Offline Edits):** If a user edits a task 5 times while offline, the queue currently has 5 separate `PUT` transactions. The backend will process them sequentially. In the future, the `TransactionQueue` could squash redundant updates for the same `entityId` before sending.
- **Backend Rewrite:** Currently debating migrating the entire API to Minimal APIs to cleanly implement this architecture from the ground up without legacy baggage.
