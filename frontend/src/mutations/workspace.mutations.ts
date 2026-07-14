import { toJS } from 'mobx'
import { getActiveRootStore } from '@/stores/root.store'
import type { RootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { WorkspaceRecord } from '@/types/workspace/workspace-record'
import type { PagedResult } from '@/types/paged-result'
import { api } from '@/lib/api-client'
import { isConnectivityError, isNotFoundError } from '@/lib/is-connectivity-error'
import { devError } from '@/sync/dev-log'
import { toast } from 'sonner'

export interface WorkspaceListFilters {
  name?: string
  owned?: boolean
  isArchived?: boolean
  direction?: 'Ascending' | 'Descending'
  cursor?: string | null
  pageSize?: number
}

export class WorkspaceMutations {
  private rootStore: RootStore

  private syncEngine: SyncEngine | undefined

  constructor(rootStore: RootStore, syncEngine?: SyncEngine) {
    this.rootStore = rootStore
    this.syncEngine = syncEngine
  }

  async fetchDetail(workspaceId: string): Promise<WorkspaceRecord> {
    const { data } = await api.get<WorkspaceRecord>(`/workspaces/${workspaceId}/me/permissions`, {
      headers: { 'X-Workspace-Id': workspaceId },
    })
    const existing = this.rootStore.workspaceStore.getById(workspaceId)
    const merged = existing ? { ...toJS(existing), ...data } : data
    this.rootStore.workspaceStore.upsert(merged)
    await this.rootStore.workspaceDB?.put(merged)
    return merged
  }

  async fetchList(filters: WorkspaceListFilters = {}): Promise<PagedResult<WorkspaceRecord>> {
    const { data } = await api.get<PagedResult<WorkspaceRecord>>('/workspaces/sync', {
      params: {
        cursor: filters.cursor ?? undefined,
        name: filters.name,
        owned: filters.owned,
        isArchived: filters.isArchived,
        direction: filters.direction ?? 'Ascending',
        pageSize: filters.pageSize,
      },
    })

    for (const item of data.items) {
      const incoming = { ...item } as unknown as WorkspaceRecord
      const existing = this.rootStore.workspaceStore.getById(incoming.id)
      const merged = existing ? { ...toJS(existing), ...incoming } : incoming
      this.rootStore.workspaceStore.upsert(merged)
      await this.rootStore.workspaceDB?.put(merged)
    }

    return data
  }

  async pin(workspaceId: string, isPinned: boolean): Promise<void> {
    const stored = this.rootStore.workspaceStore.getById(workspaceId)
    const previous = stored ? toJS(stored) : undefined

    if (stored) {
      this.rootStore.workspaceStore.upsert({ ...previous, isPinned } as WorkspaceRecord)
    }

    try {
      await api.put(`/workspaces/${workspaceId}/pin`, { isPinned }, {
        headers: { 'X-Workspace-Id': workspaceId },
      })
    } catch (err) {
      if (previous) {
        this.rootStore.workspaceStore.upsert(previous)
        await this.rootStore.workspaceDB?.put(previous)
      }
      throw err
    }
  }

  async removeFromList(workspaceId: string): Promise<void> {
    this.rootStore.workspaceStore.remove(workspaceId)
    await this.rootStore.workspaceDB?.delete(workspaceId)
  }

  async joinByCode(joinCode: string): Promise<{ workspaceId: string; membershipStatus: string; isNewMember: boolean }> {
    const { data } = await api.post('/workspaces/sync/join', { joinCode })
    return data
  }

  async create(data: { name: string; color?: string; icon?: string; description?: string; strictJoin?: boolean; theme?: string }): Promise<WorkspaceRecord> {
    const id = crypto.randomUUID()
    const traceId = crypto.randomUUID()

    const payload = {
      id,
      name: data.name,
      color: data.color ?? null,
      icon: data.icon ?? null,
      description: data.description ?? null,
      strictJoin: data.strictJoin ?? null,
      theme: data.theme ?? null,
    }

    await api.post('/workspaces/sync', payload, {
      headers: {
        'X-Client-Trace-Id': traceId,
      }
    })

    const record: WorkspaceRecord = { id, name: data.name, color: data.color, icon: data.icon } as WorkspaceRecord

    this.rootStore.workspaceStore.upsert(record)
    await this.rootStore.workspaceDB?.put(record)

    return record
  }

  async update(workspaceId: string, changes: Partial<WorkspaceRecord>): Promise<void> {
    if (!this.syncEngine) throw new Error('WorkspaceMutations.update() requires a SyncEngine — only callable from within a workspace')
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
      toast.error('Failed to save workspace locally. Please try again.')
      throw new Error(`Failed to persist workspace update locally: ${err instanceof Error ? err.message : String(err)}`)
    }

    const payload = {
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

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn('App is offline. Workspace update will sync when connection is restored.')
      return
    }

    try {
      await api.put(`/workspaces/sync/${workspaceId}`, payload, {
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
        return
      }

      this.rootStore.workspaceStore.upsert(previous)
      await this.rootStore.workspaceDB?.put(previous)
      throw err
    }
  }
}
