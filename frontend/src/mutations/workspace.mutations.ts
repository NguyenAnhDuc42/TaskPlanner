import { toJS } from 'mobx'
import type { RootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { WorkspaceRecord } from '@/types/workspace/workspace-record'
import { api } from '@/lib/api-client'
import { isConnectivityError, isNotFoundError } from '@/lib/is-connectivity-error'
import { devError } from '@/sync/dev-log'

export class WorkspaceMutations {
  private rootStore: RootStore
  private syncEngine: SyncEngine

  constructor(rootStore: RootStore, syncEngine: SyncEngine) {
    this.rootStore = rootStore
    this.syncEngine = syncEngine
  }

  // ── CREATE ──
  // Server-first: workspace initialization (statuses, folder, tasks) happens server-side,
  // so the client can't meaningfully create one offline without knowing those generated IDs.
  // Client provides the ID so it can store the record immediately after the call.
  async create(data: { name: string; color?: string; icon?: string; description?: string }): Promise<WorkspaceRecord> {
    const id = crypto.randomUUID()
    const traceId = crypto.randomUUID()

    const commandPayload = {
      id,
      name: data.name,
      color: data.color ?? null,
      icon: data.icon ?? null,
      description: data.description ?? null,
    }

    // Server-first: workspace initialization runs server-side (statuses, folder, tasks).
    // Client constructs the snippet from what it sent — no need to wait for a full record back.
    await api.post('/workspaces/sync', commandPayload, {
      headers: {
        'X-Workspace-Id': this.rootStore.currentWorkspaceId!,
        'X-Client-Trace-Id': traceId,
      }
    })

    const record: WorkspaceRecord = { id, name: data.name, color: data.color, icon: data.icon } as WorkspaceRecord

    this.rootStore.workspaceStore.upsert(record)
    await this.rootStore.workspaceDB?.put(record)

    return record
  }

  // ── UPDATE ──
  // Optimistic + queued: updates locally first, enqueues for server sync.
  // Workspace context switches require a live connection anyway, so offline
  // queuing here covers brief disconnects rather than full offline sessions.
  async update(workspaceId: string, changes: Partial<WorkspaceRecord>): Promise<void> {
    const stored = this.rootStore.workspaceStore.getById(workspaceId)
    if (!stored) throw new Error(`Workspace ${workspaceId} not found`)
    const previous = toJS(stored)

    const merged = { ...previous, ...changes }

    this.rootStore.workspaceStore.upsert(merged)
    try {
      await this.rootStore.workspaceDB?.put(merged)
    } catch (err) {
      this.rootStore.workspaceStore.upsert(previous)
      devError('[WorkspaceMutations] workspaceDB.put failed:', err)
      throw new Error(`Failed to persist workspace update locally: ${err instanceof Error ? err.message : String(err)}`)
    }

    const commandPayload = {
      name: changes.name ?? null,
      description: changes.description ?? null,
      color: changes.color ?? null,
      icon: changes.icon ?? null,
    }

    const tx = await this.syncEngine.transactionQueue.enqueue(
      'U',
      'Workspace',
      workspaceId,
      merged as unknown as Record<string, unknown>,
      previous as unknown as Record<string, unknown>
    )

    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Workspace update will sync when connection is restored.')
      return
    }

    try {
      await api.put(`/workspaces/sync/${workspaceId}`, commandPayload, {
        headers: {
          'X-Workspace-Id': workspaceId,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (isConnectivityError(err)) {
        console.warn('You are offline. Workspace update will sync when connection is restored.')
        return
      }

      this.rootStore.workspaceStore.upsert(previous)
      await this.rootStore.workspaceDB?.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }

  // ── DELETE ──
  async delete(workspaceId: string): Promise<void> {
    const stored = this.rootStore.workspaceStore.getById(workspaceId)
    if (!stored) throw new Error(`Workspace ${workspaceId} not found`)
    const previous = toJS(stored)

    this.rootStore.workspaceStore.remove(workspaceId)
    await this.rootStore.workspaceDB?.delete(workspaceId)

    try {
      await api.delete(`/workspaces/${workspaceId}`, {
        headers: { 'X-Workspace-Id': workspaceId }
      })
    } catch (err) {
      if (isNotFoundError(err)) {
        // Already deleted server-side (a retried/duplicate delete, or another client beat us to
        // it) — the desired end state is already correct, don't resurrect it locally.
        return
      }

      this.rootStore.workspaceStore.upsert(previous)
      await this.rootStore.workspaceDB?.put(previous)
      throw err
    }
  }
}
