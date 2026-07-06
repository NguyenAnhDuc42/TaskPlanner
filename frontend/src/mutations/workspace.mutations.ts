import { toJS } from 'mobx'
import type { RootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { WorkspaceRecord } from '@/types/workspace/workspace-record'
import type { WorkspaceSnippetRecord } from '@/types/workspace/workspace-snippet-record'
import type { PagedResult } from '@/types/paged-result'
import { api } from '@/lib/api-client'
import { isConnectivityError, isNotFoundError } from '@/lib/is-connectivity-error'
import { devError } from '@/sync/dev-log'

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
    // X-Workspace-Id is required here — this endpoint's route param is named "id", not
    // "workspaceId", so WorkspaceContextMiddleware's route-value fallback never resolves it.
    // Without this header every call 400s with "Workspace ID not found in context", for every
    // user, not just invited ones — the underlying cause of the "not authorized" bug report.
    const { data } = await api.get<WorkspaceRecord>(`/workspaces/${workspaceId}/me/permissions`, {
      headers: { 'X-Workspace-Id': workspaceId },
    })
    const existing = this.rootStore.workspaceStore.getById(workspaceId)
    const merged = existing ? { ...toJS(existing), ...data } : data
    this.rootStore.workspaceStore.upsert(merged)
    await this.rootStore.workspaceDB?.put(merged)
    return merged
  }

  async fetchList(filters: WorkspaceListFilters = {}): Promise<PagedResult<WorkspaceSnippetRecord>> {
    const { data } = await api.get<PagedResult<WorkspaceSnippetRecord>>('/workspaces/sync', {
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
      const record = { ...item } as unknown as WorkspaceRecord
      this.rootStore.workspaceStore.upsert(record)
      await this.rootStore.workspaceDB?.put(record)
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
      // Explicit header, not left to the interceptor's sessionStorage-active-workspace fallback —
      // pin can target any workspace in the list (from the switcher/home screen), not necessarily
      // the one currently active, so a guessed header would check membership in the wrong one.
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

  // ── JOIN BY CODE ──
  async joinByCode(joinCode: string): Promise<{ workspaceId: string; membershipStatus: string; isNewMember: boolean }> {
    const { data } = await api.post('/workspaces/sync/join', { joinCode })
    return data
  }

 
  async create(data: { name: string; color?: string; icon?: string; description?: string; strictJoin?: boolean; theme?: string }): Promise<WorkspaceRecord> {
    const id = crypto.randomUUID()
    const traceId = crypto.randomUUID()

    const commandPayload = {
      id,
      name: data.name,
      color: data.color ?? null,
      icon: data.icon ?? null,
      description: data.description ?? null,
      strictJoin: data.strictJoin ?? null,
      theme: data.theme ?? null,
    }

    await api.post('/workspaces/sync', commandPayload, {
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
