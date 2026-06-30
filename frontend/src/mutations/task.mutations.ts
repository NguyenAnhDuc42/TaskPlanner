import type { RootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { TaskRecord } from '@/types/projects/task-record'
import { Priority } from '@/types/priority'
import { api } from '@/lib/api-client'
import { devError } from '@/sync/dev-log'
import axios from 'axios'

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
    const previous = this.rootStore.taskStore.getById(taskId)
    if (!previous) throw new Error(`Task ${taskId} not found`)

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

    // 3. Enqueue transaction
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'U',
      'Task',
      taskId,
      merged as unknown as Record<string, unknown>,
      previous as unknown as Record<string, unknown>
    )

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.put(`/tasks/sync/${taskId}`, changes, {
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
    const previous = this.rootStore.taskStore.getById(taskId)
    if (!previous) throw new Error(`Task ${taskId} not found`)

    const archived = { ...previous, isArchived: true }

    // 1. Optimistic
    this.rootStore.taskStore.upsert(archived)

    // 2. Persist
    try {
      await this.rootStore.taskDB!.put(archived)
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