import type { WorkspaceRootStore } from '@/stores/workspace-root.store'
import { getActiveRootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { TaskRecord } from '@/types/projects/task-record'
import type { PendingTransaction } from '@/types/sync/transaction'
import { Priority } from '@/types/priority'
import { api } from '@/lib/api-client'
import { isConnectivityError, isNotFoundError } from "@/lib/is-connectivity-error";
import { devError } from '@/sync/dev-log'
import { toast } from 'sonner'
import { fractionalAfter } from '@/features/workspace/contents/hierarchy/utils/fractional-index'
import { toJS } from 'mobx'

const EMPTY_GUID = '00000000-0000-0000-0000-000000000000'

export class TaskMutations {
  private rootStore: WorkspaceRootStore
  private syncEngine : SyncEngine

  constructor(rootStore: WorkspaceRootStore, syncEngine: SyncEngine) {
    this.rootStore = rootStore
    this.syncEngine = syncEngine
  }

  async create(data: Omit<TaskRecord, 'id' | 'createdAt'> & { spaceId?: string | null; folderId?: string | null }): Promise<TaskRecord> {
    const id = crypto.randomUUID()
    const defaultDocumentId = crypto.randomUUID()
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

    this.rootStore.taskStore.upsert(record)

    try {
      await this.rootStore.taskDB!.put(record)
    } catch (err) {
      this.rootStore.taskStore.remove(record.id)
      devError('[TaskMutations] taskDB.put failed:', err)
      toast.error('Failed to save task locally. Please try again.')
      throw new Error(`Failed to persist task locally: ${err instanceof Error ? err.message : String(err)}`)
    }

    const payload = {
      id,
      defaultDocumentId,
      projectWorkspaceId: this.rootStore.workspaceId,
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

    const tx = await this.syncEngine.transactionQueue.enqueue(
      'C',
      'Task',
      record.id,
      payload,
      null
    )

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return record
    }

    try {
      await api.post('/tasks/sync', payload, {
        headers: {
          'X-Workspace-Id': this.rootStore.workspaceId,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (isConnectivityError(err)) {
        console.warn('You are offline. Task will sync when connection is restored.')
        return record
      }

      this.rootStore.taskStore.remove(record.id)
      await this.rootStore.taskDB!.delete(record.id)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }

    return record
  }

  async updateLocal(taskId: string, changes: Partial<TaskRecord>): Promise<{ previous: TaskRecord; tx: PendingTransaction }> {
    const stored = this.rootStore.taskStore.getById(taskId)
    if (!stored) throw new Error(`Task ${taskId} not found`)
    const previous = toJS(stored)

    const clearingStartDate = changes.startDate === null
    const clearingDueDate = changes.dueDate === null

    const merged = { ...previous, ...changes }

    this.rootStore.taskStore.upsert(merged)

    try {
      await this.rootStore.taskDB!.put(merged)
    } catch {
      this.rootStore.taskStore.upsert(previous)
      toast.error('Failed to save task locally. Please try again.')
      throw new Error('Failed to persist update locally')
    }

    const payload: Record<string, unknown> = {}
    if ('name' in changes) payload.name = changes.name
    if ('color' in changes) payload.color = changes.color
    if ('icon' in changes) payload.icon = changes.icon
    if ('statusId' in changes) payload.statusId = changes.statusId ?? EMPTY_GUID
    if ('priority' in changes) payload.priority = changes.priority
    if ('storyPoints' in changes) payload.storyPoints = changes.storyPoints
    if ('timeEstimateSeconds' in changes) payload.timeEstimateSeconds = changes.timeEstimateSeconds
    if ('orderKey' in changes) payload.orderKey = changes.orderKey
    if ('parentTaskId' in changes) payload.parentTaskId = changes.parentTaskId
    if ('spaceId' in changes) payload.spaceId = changes.spaceId
    if ('folderId' in changes) payload.folderId = changes.folderId ?? EMPTY_GUID
    if (clearingStartDate) payload.clearStartDate = true
    else if ('startDate' in changes) payload.startDate = changes.startDate
    if (clearingDueDate) payload.clearDueDate = true
    else if ('dueDate' in changes) payload.dueDate = changes.dueDate

    const tx = await this.syncEngine.transactionQueue.enqueue(
      'U',
      'Task',
      taskId,
      payload,
      previous as unknown as Record<string, unknown>
    )

    return { previous, tx }
  }

  async update(taskId: string, changes: Partial<TaskRecord>): Promise<void> {
    const { previous, tx } = await this.updateLocal(taskId, changes)

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.put(`/tasks/sync/${taskId}`, tx.data, {
        headers: {
          'X-Workspace-Id': this.rootStore.workspaceId,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (isConnectivityError(err)) {
        console.warn('You are offline. Update will sync when connection is restored.')
        return
      }

      this.rootStore.taskStore.upsert(previous)
      await this.rootStore.taskDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }

  async deleteLocal(taskId: string): Promise<{ previous: TaskRecord; tx: PendingTransaction }> {
    const stored = this.rootStore.taskStore.getById(taskId)
    if (!stored) throw new Error(`Task ${taskId} not found`)
    const previous = toJS(stored)

    this.rootStore.taskStore.remove(taskId)

    try {
      await this.rootStore.taskDB!.delete(taskId)
    } catch {
      this.rootStore.taskStore.upsert(previous)
      toast.error('Failed to delete task locally. Please try again.')
      throw new Error('Failed to persist delete locally')
    }

    const tx = await this.syncEngine.transactionQueue.enqueue(
      'D',
      'Task',
      taskId,
      { id: taskId },
      previous as unknown as Record<string, unknown>
    )

    return { previous, tx }
  }

  async delete(taskId: string): Promise<void> {
    const { previous, tx } = await this.deleteLocal(taskId)

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.delete(`/tasks/sync/${taskId}`, {
        headers: {
          'X-Workspace-Id': this.rootStore.workspaceId,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (isConnectivityError(err)) {
        console.warn('You are offline. Deletion will sync when connection is restored.')
        return
      }

      if (isNotFoundError(err)) {
        await this.syncEngine.transactionQueue.dequeue(tx.id)
        return
      }

      this.rootStore.taskStore.upsert(previous)
      await this.rootStore.taskDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }
}
