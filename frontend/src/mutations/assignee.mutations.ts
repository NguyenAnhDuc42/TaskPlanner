import type { WorkspaceRootStore } from '@/stores/workspace-root.store'
import { getActiveRootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { AssigneeRecord } from '@/types/projects/assignee-record'
import { api } from '@/lib/api-client'
import { isConnectivityError, isNotFoundError } from "@/lib/is-connectivity-error";
import { devError } from '@/sync/dev-log'
import { toJS } from 'mobx'
import { toast } from 'sonner'

export class AssigneeMutations {
  private rootStore: WorkspaceRootStore
  private syncEngine: SyncEngine

  constructor(rootStore: WorkspaceRootStore, syncEngine: SyncEngine) {
    this.rootStore = rootStore
    this.syncEngine = syncEngine
  }

  async create(taskId: string, memberId: string): Promise<AssigneeRecord> {
    const id = crypto.randomUUID()

    const record: AssigneeRecord = {
      id,
      taskId,
      workspaceMemberId: memberId,
    }

    this.rootStore.assigneeStore.upsert(record)

    try {
      await this.rootStore.assigneeDB!.put(record)
    } catch (err) {
      this.rootStore.assigneeStore.remove(record.id)
      devError('[AssigneeMutations] assigneeDB.put failed:', err)
      toast.error('Failed to save assignee locally. Please try again.')
      throw new Error(`Failed to persist assignee locally: ${err instanceof Error ? err.message : String(err)}`)
    }

    const payload = {
      id,
      taskId,
      memberId,
    }

    const tx = await this.syncEngine.transactionQueue.enqueue('C', 'Assignee', record.id, payload, null)

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return record
    }

    try {
      await api.post('/assignees/sync', payload, {
        headers: {
          'X-Workspace-Id': this.rootStore.workspaceId,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (isConnectivityError(err)) {
        console.warn('You are offline. Assignee will sync when connection is restored.')
        return record
      }

      this.rootStore.assigneeStore.remove(record.id)
      await this.rootStore.assigneeDB!.delete(record.id)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }

    return record
  }

  async delete(assigneeId: string): Promise<void> {
    const stored = this.rootStore.assigneeStore.getById(assigneeId)
    if (!stored) throw new Error(`Assignee ${assigneeId} not found`)
    const previous = toJS(stored)

    this.rootStore.assigneeStore.remove(assigneeId)

    try {
      await this.rootStore.assigneeDB!.delete(assigneeId)
    } catch {
      this.rootStore.assigneeStore.upsert(previous)
      toast.error('Failed to delete assignee locally. Please try again.')
      throw new Error('Failed to persist delete locally')
    }

    const tx = await this.syncEngine.transactionQueue.enqueue(
      'D',
      'Assignee',
      assigneeId,
      { id: assigneeId },
      previous as unknown as Record<string, unknown>
    )

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.delete(`/assignees/sync/${assigneeId}`, {
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

      this.rootStore.assigneeStore.upsert(previous)
      await this.rootStore.assigneeDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }
}
