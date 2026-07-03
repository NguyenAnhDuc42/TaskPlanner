import type { RootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { TaskRecord } from '@/types/projects/task-record'
import type { PendingTransaction } from '@/types/sync/transaction'
import { Priority } from '@/types/priority'
import { api } from '@/lib/api-client'
import { isConnectivityError } from "@/lib/is-connectivity-error";
import { devError } from '@/sync/dev-log'
import { fractionalAfter } from '@/features/workspace/contents/hierarchy/utils/fractional-index'
import { toJS } from 'mobx'

// ProjectTask.Update() on the backend can't distinguish "not touched" (Guid? = null) from
// "explicitly cleared" for folderId any other way, so it uses this sentinel — mirrors the same
// convention already used server-side for statusId.
const EMPTY_GUID = '00000000-0000-0000-0000-000000000000'

export class TaskMutations {
  private rootStore : RootStore
  private syncEngine : SyncEngine
  
  constructor(rootStore: RootStore, syncEngine: SyncEngine) {
    this.rootStore = rootStore
    this.syncEngine = syncEngine
  }

  // ── CREATE ──
  async create(data: Omit<TaskRecord, 'id' | 'createdAt'> & { spaceId?: string | null; folderId?: string | null }): Promise<TaskRecord> {
    const id = crypto.randomUUID()
    const defaultDocumentId = crypto.randomUUID()
    // Unlike Space/Folder, the backend trusts whatever orderKey the client sends for Task create
    // (no server-side FractionalIndex.SafeAfter recompute) — Date.now().toString(36) was NOT a
    // valid fractional-indexing key, so any reorder involving a task created this way broke
    // immediately and permanently (the bad key round-tripped straight to the server). Must use
    // the same fractional-indexing scheme as everywhere else, positioned after the current last
    // sibling in the same scope (same parentTaskId, else same folder, else same space).
    const siblings = this.rootStore.taskStore.all.filter((t) => {
      if (data.parentTaskId) return t.parentTaskId === data.parentTaskId
      if (data.folderId) return t.folderId === data.folderId && !t.parentTaskId
      return t.spaceId === data.spaceId && !t.folderId && !t.parentTaskId
    })
    const maxSiblingKey = siblings.reduce<string | null>(
      (max, t) => (t.orderKey && (!max || t.orderKey > max) ? t.orderKey : max),
      null,
    )
    const orderKey = data.orderKey ?? fractionalAfter(maxSiblingKey)
    const slug = `${data.name.toLowerCase().replace(/[^a-z0-9]+/g, '-').slice(0, 40)}-${id.slice(0, 8)}`

    const record: TaskRecord = {
      ...data,
      id,
      createdAt: new Date().toISOString(),
      defaultDocumentId,
      orderKey,
    }

    // 1. Optimistic — user sees it instantly
    this.rootStore.taskStore.upsert(record)

    // 2. Persist to IndexedDB
    try {
      await this.rootStore.taskDB!.put(record)
    } catch (err) {
      this.rootStore.taskStore.remove(record.id)
      devError('[TaskMutations] taskDB.put failed:', err)
      throw new Error(`Failed to persist task locally: ${err instanceof Error ? err.message : String(err)}`)
    }

    // CreateTaskCommand shape on the backend — field names must match exactly,
    // TaskRecord's shape is for local/read state, not the wire format. This is
    // what gets enqueued AND sent, so offline-deferred sends use the same
    // correct shape as an immediate online send.
    const commandPayload = {
      id,
      defaultDocumentId,
      projectWorkspaceId: this.rootStore.currentWorkspaceId,
      projectSpaceId: data.spaceId ?? null,
      projectFolderId: data.folderId ?? null,
      name: data.name,
      slug,
      color: data.color ?? null,
      icon: data.icon ?? null,
      statusId: data.statusId ?? null,
      priority: data.priority ?? Priority.Low,
      orderKey,
      parentTaskId: data.parentTaskId ?? null,
    }

    // 3. Enqueue transaction (tracks in-flight state)
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'C',
      'Task',
      record.id,
      commandPayload,
      null
    )

    // 4. Synchronous API call (Hybrid Approach)
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return record
    }

    try {
      await api.post('/tasks/sync', commandPayload, {
        headers: {
          'X-Workspace-Id': this.rootStore.currentWorkspaceId!,
          'X-Client-Trace-Id': tx.id,
        }
      })

      // Success: transaction stays in queue until SignalR confirms it via Delta
    } catch (err) {
      if (isConnectivityError(err)) {
        // Network Error (Offline or Server Down)
        // DO NOT ROLLBACK! Keep it in the queue for the background sync.
        console.warn('You are offline. Task will sync when connection is restored.')
        return record
      }

      // Explicit Server Rejection (e.g., 400 Bad Request, 403 Forbidden)
      // Fallback/Rollback immediately
      this.rootStore.taskStore.remove(record.id)
      await this.rootStore.taskDB!.delete(record.id)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }

    return record
  }

  // ── UPDATE (local-only — store + IndexedDB + enqueue, no network call) ──
  // The building block both update() and the debounced field-edit flow use. Local writes are
  // cheap and always instant — no reason to debounce them. Squashing multiple rapid updates
  // into one network call is TransactionQueue's job already (squash() merges same-entity U
  // transactions before a flush sends them) — no need to duplicate that merge logic in a
  // component. Callers that want fast per-field UI updates without spamming the network should
  // call this on every change and debounce a `syncEngine.flushQueue()` trigger instead of
  // debouncing this call itself.
  async updateLocal(taskId: string, changes: Partial<TaskRecord>): Promise<{ previous: TaskRecord; tx: PendingTransaction }> {
    const stored = this.rootStore.taskStore.getById(taskId)
    if (!stored) throw new Error(`Task ${taskId} not found`)
    // MobX observable.map values are deep-observable Proxies — must unwrap
    // before they touch IndexedDB or structuredClone throws "could not be cloned".
    const previous = toJS(stored)

    // startDate/dueDate === null means "explicitly clear"; undefined means "not touched".
    // ProjectTask.Update() on the backend only clears on the boolean flag — passing
    // startDate: null alone is treated as a no-op, not a clear.
    const clearingStartDate = changes.startDate === null
    const clearingDueDate = changes.dueDate === null

    const merged = { ...previous, ...changes }

    // 1. Optimistic
    this.rootStore.taskStore.upsert(merged)

    // 2. Persist
    try {
      await this.rootStore.taskDB!.put(merged)
    } catch {
      this.rootStore.taskStore.upsert(previous)
      throw new Error('Failed to persist update locally')
    }

    // UpdateTaskCommand wire shape — built explicitly (not just forwarding `changes`)
    // so clearStartDate/clearDueDate are correctly set. Used for BOTH the enqueue and
    // an immediate send (see update() below), so offline-deferred sends (via batch flush,
    // deserialized straight into UpdateTaskCommand) carry the same correct shape as an
    // online send — enqueueing `merged` (TaskRecord shape) instead was exactly the bug
    // already fixed for Create (see FRONTEND_SYNC_CONTEXT.md §7 bug 1).
    //
    // Only include keys the caller actually touched — TransactionQueue.squash()'s U+U
    // merge does `{...acc, ...u.data}`, so a key present-but-unchanged (even as `null`)
    // would clobber a real change from an earlier queued update to the same field.
    const commandPayload: Record<string, unknown> = {}
    if ('name' in changes) commandPayload.name = changes.name
    if ('color' in changes) commandPayload.color = changes.color
    if ('icon' in changes) commandPayload.icon = changes.icon
    // statusId: null means "explicitly clear" (e.g. dragging a task to the board's
    // "Unclassified" column) — translated to EMPTY_GUID for the same reason folderId is below.
    if ('statusId' in changes) commandPayload.statusId = changes.statusId ?? EMPTY_GUID
    if ('priority' in changes) commandPayload.priority = changes.priority
    if ('storyPoints' in changes) commandPayload.storyPoints = changes.storyPoints
    if ('timeEstimateSeconds' in changes) commandPayload.timeEstimateSeconds = changes.timeEstimateSeconds
    if ('orderKey' in changes) commandPayload.orderKey = changes.orderKey
    if ('parentTaskId' in changes) commandPayload.parentTaskId = changes.parentTaskId
    // Reparenting across Space/Folder boundaries (drag-and-drop in the hierarchy sidebar).
    // spaceId has no "clear" sentinel — a task always belongs to some space. folderId: null
    // means "move directly under the space, no folder" — translated to EMPTY_GUID since the
    // backend can't tell null-as-not-touched apart from null-as-clear otherwise.
    if ('spaceId' in changes) commandPayload.spaceId = changes.spaceId
    if ('folderId' in changes) commandPayload.folderId = changes.folderId ?? EMPTY_GUID
    if (clearingStartDate) commandPayload.clearStartDate = true
    else if ('startDate' in changes) commandPayload.startDate = changes.startDate
    if (clearingDueDate) commandPayload.clearDueDate = true
    else if ('dueDate' in changes) commandPayload.dueDate = changes.dueDate

    // 3. Enqueue transaction — no network call here
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'U',
      'Task',
      taskId,
      commandPayload,
      previous as unknown as Record<string, unknown>
    )

    return { previous, tx }
  }

  // ── UPDATE (immediate — local write + queue + synchronous send) ──
  // Use for single deliberate actions (e.g. a modal Save button). For rapid per-field edits
  // (typing, quick picker changes), call updateLocal() on every change instead and debounce
  // a syncEngine.flushQueue() trigger — see TaskDetailCanvas's useDebouncedTaskUpdate.
  async update(taskId: string, changes: Partial<TaskRecord>): Promise<void> {
    const { previous, tx } = await this.updateLocal(taskId, changes)

    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.put(`/tasks/sync/${taskId}`, tx.data, {
        headers: {
          'X-Workspace-Id': this.rootStore.currentWorkspaceId!,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (isConnectivityError(err)) {
        // Network Error (Offline) -> Keep in queue
        console.warn('You are offline. Update will sync when connection is restored.')
        return
      }

      // Explicit Server Rejection -> Rollback
      this.rootStore.taskStore.upsert(previous)
      await this.rootStore.taskDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }

  // ── DELETE (local-only — store + IndexedDB + enqueue, no network call) ──
  // Building block for delete(). Batch deletes (e.g. a "delete selected" toolbar action) should
  // call this once per task, then a single shared flushNow()/scheduleFlush() — N deletes queued,
  // one POST /api/sync/batch call — instead of N calls to delete() each firing its own request.
  async deleteLocal(taskId: string): Promise<{ previous: TaskRecord; tx: PendingTransaction }> {
    const stored = this.rootStore.taskStore.getById(taskId)
    if (!stored) throw new Error(`Task ${taskId} not found`)
    const previous = toJS(stored)

    // 1. Eager local removal — don't wait for the Delta confirmation
    this.rootStore.taskStore.remove(taskId)

    // 2. Persist
    try {
      await this.rootStore.taskDB!.delete(taskId)
    } catch {
      this.rootStore.taskStore.upsert(previous)
      throw new Error('Failed to persist delete locally')
    }

    // 3. Enqueue — no network call here
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'D',
      'Task',
      taskId,
      { id: taskId },
      previous as unknown as Record<string, unknown>
    )

    return { previous, tx }
  }

  // ── DELETE (immediate — local write + queue + synchronous send) ──
  async delete(taskId: string): Promise<void> {
    const { previous, tx } = await this.deleteLocal(taskId)

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.delete(`/tasks/sync/${taskId}`, {
        headers: {
          'X-Workspace-Id': this.rootStore.currentWorkspaceId!,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (isConnectivityError(err)) {
        // Network Error (Offline) -> Keep in queue
        console.warn('You are offline. Deletion will sync when connection is restored.')
        return
      }

      // Explicit Server Rejection -> Rollback
      this.rootStore.taskStore.upsert(previous)
      await this.rootStore.taskDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }
}

