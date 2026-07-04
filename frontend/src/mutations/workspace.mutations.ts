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
  private syncEngine: SyncEngine

  constructor(rootStore: RootStore, syncEngine: SyncEngine) {
    this.rootStore = rootStore
    this.syncEngine = syncEngine
  }

  // ── FETCH DETAIL ──
  // Per-workspace permissions (canEdit/canInvite/canManageMembers/joinCode etc) — a separate
  // concern from synced entity data, not part of Bootstrap. No new-pattern endpoint needed
  // (same reasoning as pin()): plain read, reuses the existing legacy REST route directly.
  async fetchDetail(workspaceId: string): Promise<WorkspaceRecord> {
    const { data } = await api.get<WorkspaceRecord>(`/workspaces/${workspaceId}/me/permissions`)
    this.rootStore.workspaceStore.upsert(data)
    await this.rootStore.workspaceDB?.put(data)
    return data
  }

  // ── FETCH LIST ──
  // Read-side query, not queued/offline — matches the backend's own "Workspace bypasses
  // Bootstrap/Delta entirely" design (see SYNC_SCENARIOS.md). Upserts every returned row into
  // the store + IndexedDB so the switcher/home screen reflect it immediately.
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

  // ── PIN ──
  // Optimistic, no queue — a personal per-member flag, not a synced/shared entity, same
  // reasoning as Favorite. No new-pattern endpoint exists for this yet; reuses the legacy
  // per-workspace REST route directly since it's a trivial one-field update.
  async pin(workspaceId: string, isPinned: boolean): Promise<void> {
    const stored = this.rootStore.workspaceStore.getById(workspaceId)
    const previous = stored ? toJS(stored) : undefined

    if (stored) {
      this.rootStore.workspaceStore.upsert({ ...previous, isPinned } as WorkspaceRecord)
    }

    try {
      await api.put(`/workspaces/${workspaceId}/pin`, { isPinned })
    } catch (err) {
      if (previous) {
        this.rootStore.workspaceStore.upsert(previous)
        await this.rootStore.workspaceDB?.put(previous)
      }
      throw err
    }
  }

  // ── JOIN BY CODE ──
  async joinByCode(joinCode: string): Promise<{ workspaceId: string; membershipStatus: string; isNewMember: boolean }> {
    const { data } = await api.post('/workspaces/sync/join', { joinCode })
    return data
  }

  // ── CREATE ──
  // Server-first: workspace initialization (statuses, folder, tasks) happens server-side,
  // so the client can't meaningfully create one offline without knowing those generated IDs.
  // Client provides the ID so it can store the record immediately after the call.
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

    // Server-first: workspace initialization runs server-side (statuses, folder, tasks).
    // No X-Workspace-Id here — this endpoint isn't workspace-scoped (there is no workspace
    // yet), and CreateWorkspaceEndpoint only requires X-Client-Trace-Id.
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
