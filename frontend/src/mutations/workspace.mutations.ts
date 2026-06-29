import type { RootStore } from '@/stores/root.store'
import type { WorkspaceRecord } from '@/types/workspace/workspace-record'
import { api } from '@/lib/api-client'

export class WorkspaceMutations {
  private rootStore : RootStore
  
  constructor(rootStore: RootStore) {
    this.rootStore = rootStore
  }

  // ── CREATE ──
  async create(data: Omit<WorkspaceRecord, 'id'>): Promise<WorkspaceRecord> {
    // 1. Synchronous API call
    // Note: We DO NOT use transactionQueue for Workspaces because joining/creating 
    // requires server-side validation (limits, permissions, billing, etc.)
    const res = await api.post('/workspaces', data)
    
    // Assuming backend returns the fully created record with ID
    const record: WorkspaceRecord = res.data

    // 2. Update local Read-Replica (Cache)
    this.rootStore.workspaceStore.upsert(record)
    await this.rootStore.workspaceDB!.put(record)

    return record
  }

  // ── UPDATE ──
  async update(workspaceId: string, changes: Partial<WorkspaceRecord>): Promise<void> {
    const previous = this.rootStore.workspaceStore.getById(workspaceId)
    if (!previous) throw new Error(`Workspace ${workspaceId} not found`)

    // 1. Synchronous API call
    await api.put(`/workspaces/${workspaceId}`, changes)

    // 2. Update local Read-Replica
    const merged = { ...previous, ...changes }
    this.rootStore.workspaceStore.upsert(merged)
    await this.rootStore.workspaceDB!.put(merged)
  }

  // ── DELETE ──
  async delete(workspaceId: string): Promise<void> {
    const previous = this.rootStore.workspaceStore.getById(workspaceId)
    if (!previous) throw new Error(`Workspace ${workspaceId} not found`)

    // 1. Synchronous API call
    await api.delete(`/workspaces/${workspaceId}`)

    // 2. Update local Read-Replica
    this.rootStore.workspaceStore.remove(workspaceId)
    await this.rootStore.workspaceDB!.delete(workspaceId)
  }
}
