import type { WorkspaceRootStore } from '@/stores/workspace-root.store'
import { getActiveRootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { SpaceRecord } from '@/types/projects/space-record'
import type { PendingTransaction } from '@/types/sync/transaction'
import { api } from '@/lib/api-client'
import { isConnectivityError, isNotFoundError } from "@/lib/is-connectivity-error";
import { devError } from '@/sync/dev-log'
import { fractionalAfter } from '@/features/workspace/contents/hierarchy/utils/fractional-index'
import { toJS } from 'mobx'
import { toast } from 'sonner'

export class SpaceMutations {
  private rootStore: WorkspaceRootStore
  private syncEngine: SyncEngine

  constructor(rootStore: WorkspaceRootStore, syncEngine: SyncEngine) {
    this.rootStore = rootStore
    this.syncEngine = syncEngine
  }

  async create(data: Omit<SpaceRecord, 'id' | 'defaultDocumentId'> & { isPrivate: boolean }): Promise<SpaceRecord> {
    const id = crypto.randomUUID()
    const defaultDocumentId = crypto.randomUUID()

    const maxSiblingKey = this.rootStore.spaceStore.all.reduce<string | null>(
      (max, s) => (s.orderKey && (!max || s.orderKey > max) ? s.orderKey : max),
      null,
    )
    const orderKey = data.orderKey ?? fractionalAfter(maxSiblingKey)

    const record: SpaceRecord = { ...data, id, defaultDocumentId, orderKey }

    this.rootStore.spaceStore.upsert(record)

    try {
      await this.rootStore.spaceDB!.put(record)
    } catch (err) {
      this.rootStore.spaceStore.remove(record.id)
      devError('[SpaceMutations] spaceDB.put failed:', err)
      toast.error('Failed to save space locally. Please try again.')
      throw new Error(`Failed to persist space locally: ${err instanceof Error ? err.message : String(err)}`)
    }

    const payload = {
      id,
      defaultDocumentId,
      name: data.name,
      color: data.color ?? null,
      icon: data.icon ?? null,
      isPrivate: data.isPrivate,
    }

    const tx = await this.syncEngine.transactionQueue.enqueue('C', 'Space', record.id, payload, null)

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return record
    }

    try {
      await api.post('/spaces/sync', payload, {
        headers: {
          'X-Workspace-Id': this.rootStore.workspaceId,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (isConnectivityError(err)) {
        console.warn('You are offline. Space will sync when connection is restored.')
        return record
      }

      this.rootStore.spaceStore.remove(record.id)
      await this.rootStore.spaceDB!.delete(record.id)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }

    return record
  }

  async updateLocal(spaceId: string, changes: Partial<SpaceRecord>): Promise<{ previous: SpaceRecord; tx: PendingTransaction }> {
    const stored = this.rootStore.spaceStore.getById(spaceId)
    if (!stored) throw new Error(`Space ${spaceId} not found`)
    const previous = toJS(stored)

    const merged = { ...previous, ...changes }

    this.rootStore.spaceStore.upsert(merged)

    try {
      await this.rootStore.spaceDB!.put(merged)
    } catch {
      this.rootStore.spaceStore.upsert(previous)
      toast.error('Failed to save space locally. Please try again.')
      throw new Error('Failed to persist update locally')
    }

    const tx = await this.syncEngine.transactionQueue.enqueue(
      'U',
      'Space',
      spaceId,
      merged as unknown as Record<string, unknown>,
      previous as unknown as Record<string, unknown>
    )

    return { previous, tx }
  }

  async update(spaceId: string, changes: Partial<SpaceRecord>): Promise<void> {
    const { previous, tx } = await this.updateLocal(spaceId, changes)

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.put(`/spaces/sync/${spaceId}`, changes, {
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

      this.rootStore.spaceStore.upsert(previous)
      await this.rootStore.spaceDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }

  async delete(spaceId: string): Promise<void> {
    const stored = this.rootStore.spaceStore.getById(spaceId)
    if (!stored) throw new Error(`Space ${spaceId} not found`)
    const previous = toJS(stored)

    const folderIds = this.rootStore.folderStore.getBySpace(spaceId).map(f => f.id)
    const taskIds = this.rootStore.taskStore.getBySpace(spaceId).map(t => t.id)
    const statusIds = this.rootStore.statusStore.getBySpace(spaceId).map(s => s.id)
    const documentIds = this.rootStore.documentStore.all.filter(d => d.spaceId === spaceId).map(d => d.id)

    await this.syncEngine.transactionQueue.cancelByEntityIds([...folderIds, ...taskIds, ...documentIds])

    for (const fid of folderIds) {
      this.rootStore.folderStore.remove(fid)
      await this.rootStore.folderDB!.delete(fid)
    }
    for (const tid of taskIds) {
      this.rootStore.taskStore.remove(tid)
      await this.rootStore.taskDB!.delete(tid)
    }
    for (const sid of statusIds) {
      this.rootStore.statusStore.remove(sid)
    }
    this.rootStore.documentStore.removeMany(documentIds)
    for (const did of documentIds) this.rootStore.documentBlockStore.removeByDocument(did)
    await this.rootStore.documentDB!.deleteMany(documentIds)
    await this.rootStore.documentBlockDB!.deleteByDocumentIds(documentIds)

    this.rootStore.spaceStore.remove(spaceId)

    try {
      await this.rootStore.spaceDB!.delete(spaceId)
    } catch {
      this.rootStore.spaceStore.upsert(previous)
      toast.error('Failed to delete space locally. Please try again.')
      throw new Error('Failed to persist delete locally')
    }

    const tx = await this.syncEngine.transactionQueue.enqueue(
      'D',
      'Space',
      spaceId,
      { id: spaceId },
      previous as unknown as Record<string, unknown>
    )

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.delete(`/spaces/sync/${spaceId}`, {
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

      this.rootStore.spaceStore.upsert(previous)
      await this.rootStore.spaceDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }
}
