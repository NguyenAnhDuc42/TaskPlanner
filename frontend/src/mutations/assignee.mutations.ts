import type { RootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { AssigneeRecord } from '@/types/projects/assignee-record'
import { api } from '@/lib/api-client'
import { devError } from '@/sync/dev-log'
import axios from 'axios'
import { toJS } from 'mobx'

// No update() — assignment is a binary relationship (assigned or not), matching the
// backend's single-entity Create/Delete-only slices (no legacy diff/changeset endpoint).
export class AssigneeMutations {
  private rootStore: RootStore
  private syncEngine: SyncEngine

  constructor(rootStore: RootStore, syncEngine: SyncEngine) {
    this.rootStore = rootStore
    this.syncEngine = syncEngine
  }

  // ── CREATE ──
  async create(taskId: string, memberId: string): Promise<AssigneeRecord> {
    const id = crypto.randomUUID()

    const record: AssigneeRecord = {
      id,
      taskId,
      workspaceMemberId: memberId,
    }

    // 1. Optimistic
    this.rootStore.assigneeStore.upsert(record)

    // 2. Persist
    try {
      await this.rootStore.assigneeDB!.put(record)
    } catch (err) {
      this.rootStore.assigneeStore.remove(record.id)
      devError('[AssigneeMutations] assigneeDB.put failed:', err)
      throw new Error(`Failed to persist assignee locally: ${err instanceof Error ? err.message : String(err)}`)
    }

    // CreateAssigneeCommand wire shape
    const commandPayload = {
      id,
      taskId,
      memberId,
    }

    // 3. Enqueue transaction
    const tx = await this.syncEngine.transactionQueue.enqueue('C', 'Assignee', record.id, commandPayload, null)

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return record
    }

    try {
      await api.post('/assignees/sync', commandPayload, {
        headers: {
          'X-Workspace-Id': this.rootStore.currentWorkspaceId!,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (axios.isAxiosError(err) && !err.response) {
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

  // ── DELETE ──
  async delete(assigneeId: string): Promise<void> {
    const stored = this.rootStore.assigneeStore.getById(assigneeId)
    if (!stored) throw new Error(`Assignee ${assigneeId} not found`)
    const previous = toJS(stored)

    // 1. Eager local removal
    this.rootStore.assigneeStore.remove(assigneeId)

    // 2. Persist
    try {
      await this.rootStore.assigneeDB!.delete(assigneeId)
    } catch {
      this.rootStore.assigneeStore.upsert(previous)
      throw new Error('Failed to persist delete locally')
    }

    // 3. Enqueue
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'D',
      'Assignee',
      assigneeId,
      { id: assigneeId },
      previous as unknown as Record<string, unknown>
    )

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.delete(`/assignees/sync/${assigneeId}`, {
        headers: {
          'X-Workspace-Id': this.rootStore.currentWorkspaceId!,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (axios.isAxiosError(err) && !err.response) {
        console.warn('You are offline. Deletion will sync when connection is restored.')
        return
      }

      this.rootStore.assigneeStore.upsert(previous)
      await this.rootStore.assigneeDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }
}
