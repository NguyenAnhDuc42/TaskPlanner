import type { RootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { TaskRecord } from '@/types/projects/task-record'
import { api } from '@/lib/api-client'
import axios from 'axios'

export class TaskMutations {
  private rootStore : RootStore
  private syncEngine : SyncEngine
  
  constructor(rootStore: RootStore, syncEngine: SyncEngine) {
    this.rootStore = rootStore
    this.syncEngine = syncEngine
  }

  // ── CREATE ──
  async create(data: Omit<TaskRecord, 'id' | 'createdAt'>): Promise<TaskRecord> {
    const record: TaskRecord = {
      ...data,
      id: crypto.randomUUID(),
      createdAt: new Date().toISOString(),
    }

    // 1. Optimistic — user sees it instantly
    this.rootStore.taskStore.upsert(record)

    // 2. Persist to IndexedDB
    try {
      await this.rootStore.taskDB!.put(record)
    } catch {
      this.rootStore.taskStore.remove(record.id)
      throw new Error('Failed to persist task locally')
    }

    // 3. Enqueue transaction (tracks in-flight state)
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'C',
      'Task',
      record.id,
      record as unknown as Record<string, unknown>,
      null
    )

    // 4. Synchronous API call (Hybrid Approach)
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return record
    }

    try {
      await api.post('/tasks', record, {
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
      await api.put(`/tasks/${taskId}`, changes, {
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
      await api.delete(`/tasks/${taskId}`, {
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