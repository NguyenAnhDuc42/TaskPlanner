import type { RootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { TaskRecord } from '@/types/projects/task-record'
import { Priority } from '@/types/priority'
import { api } from '@/lib/api-client'
import { devError } from '@/sync/dev-log'
import axios from 'axios'
import { toJS } from 'mobx'

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
    const orderKey = data.orderKey ?? Date.now().toString(36)
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
      if (axios.isAxiosError(err) && !err.response) {
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

  // ── UPDATE ──
  async update(taskId: string, changes: Partial<TaskRecord>): Promise<void> {
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
    // the immediate send, so offline-deferred sends (via batch flush, deserialized
    // straight into UpdateTaskCommand) carry the same correct shape as an online send —
    // enqueueing `merged` (TaskRecord shape) instead was exactly the bug already fixed
    // for Create (see FRONTEND_SYNC_CONTEXT.md §7 bug 1).
    //
    // Only include keys the caller actually touched — TransactionQueue.squash()'s U+U
    // merge does `{...acc, ...u.data}`, so a key present-but-unchanged (even as `null`)
    // would clobber a real change from an earlier queued update to the same field.
    const commandPayload: Record<string, unknown> = {}
    if ('name' in changes) commandPayload.name = changes.name
    if ('color' in changes) commandPayload.color = changes.color
    if ('icon' in changes) commandPayload.icon = changes.icon
    if ('statusId' in changes) commandPayload.statusId = changes.statusId
    if ('priority' in changes) commandPayload.priority = changes.priority
    if ('storyPoints' in changes) commandPayload.storyPoints = changes.storyPoints
    if ('timeEstimateSeconds' in changes) commandPayload.timeEstimateSeconds = changes.timeEstimateSeconds
    if ('orderKey' in changes) commandPayload.orderKey = changes.orderKey
    if ('parentTaskId' in changes) commandPayload.parentTaskId = changes.parentTaskId
    if (clearingStartDate) commandPayload.clearStartDate = true
    else if ('startDate' in changes) commandPayload.startDate = changes.startDate
    if (clearingDueDate) commandPayload.clearDueDate = true
    else if ('dueDate' in changes) commandPayload.dueDate = changes.dueDate

    // 3. Enqueue transaction
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'U',
      'Task',
      taskId,
      commandPayload,
      previous as unknown as Record<string, unknown>
    )

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.put(`/tasks/sync/${taskId}`, commandPayload, {
        headers: {
          'X-Workspace-Id': this.rootStore.currentWorkspaceId!,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (axios.isAxiosError(err) && !err.response) {
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

  // ── DELETE ──
  async delete(taskId: string): Promise<void> {
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

    // 3. Enqueue
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'D',
      'Task',
      taskId,
      { id: taskId },
      previous as unknown as Record<string, unknown>
    )

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
      if (axios.isAxiosError(err) && !err.response) {
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

