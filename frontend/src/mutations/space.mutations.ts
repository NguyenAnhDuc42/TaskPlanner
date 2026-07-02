import type { RootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { SpaceRecord } from '@/types/projects/space-record'
import type { PendingTransaction } from '@/types/sync/transaction'
import { api } from '@/lib/api-client'
import { devError } from '@/sync/dev-log'
import { fractionalAfter } from '@/features/workspace/contents/hierarchy/utils/fractional-index'
import axios from 'axios'
import { toJS } from 'mobx'

export class SpaceMutations {
  private rootStore: RootStore
  private syncEngine: SyncEngine

  constructor(rootStore: RootStore, syncEngine: SyncEngine) {
    this.rootStore = rootStore
    this.syncEngine = syncEngine
  }

  // ── CREATE ──
  async create(data: Omit<SpaceRecord, 'id' | 'defaultDocumentId'> & { isPrivate: boolean }): Promise<SpaceRecord> {
    const id = crypto.randomUUID()
    const defaultDocumentId = crypto.randomUUID()

    // Same reasoning as FolderMutations.create() — the backend recomputes its own authoritative
    // orderKey server-side and the Delta will overwrite this, but the optimistic record needs a
    // valid fractional-indexing key in the meantime or any reorder attempted before that Delta
    // lands breaks immediately.
    const maxSiblingKey = this.rootStore.spaceStore.all.reduce<string | null>(
      (max, s) => (s.orderKey && (!max || s.orderKey > max) ? s.orderKey : max),
      null,
    )
    const orderKey = data.orderKey ?? fractionalAfter(maxSiblingKey)

    const record: SpaceRecord = { ...data, id, defaultDocumentId, orderKey }

    // 1. Optimistic
    this.rootStore.spaceStore.upsert(record)

    // 2. Persist
    try {
      await this.rootStore.spaceDB!.put(record)
    } catch (err) {
      this.rootStore.spaceStore.remove(record.id)
      devError('[SpaceMutations] spaceDB.put failed:', err)
      throw new Error(`Failed to persist space locally: ${err instanceof Error ? err.message : String(err)}`)
    }

    // CreateSpaceCommand wire shape — built before enqueue, same as TaskMutations.create
    const commandPayload = {
      id,
      defaultDocumentId,
      name: data.name,
      color: data.color ?? null,
      icon: data.icon ?? null,
      isPrivate: data.isPrivate,
    }

    // 3. Enqueue transaction
    const tx = await this.syncEngine.transactionQueue.enqueue('C', 'Space', record.id, commandPayload, null)

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return record
    }

    try {
      await api.post('/spaces/sync', commandPayload, {
        headers: {
          'X-Workspace-Id': this.rootStore.currentWorkspaceId!,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (axios.isAxiosError(err) && !err.response) {
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

  // ── UPDATE (local-only — store + IndexedDB + enqueue, no network call) ──
  // Building block for update(). Call this on every rapid field edit and debounce a
  // syncEngine.flushQueue() trigger instead of debouncing this call — TransactionQueue.squash()
  // already merges multiple pending updates for the same space into one send. See
  // TaskMutations.updateLocal() for the full rationale.
  async updateLocal(spaceId: string, changes: Partial<SpaceRecord>): Promise<{ previous: SpaceRecord; tx: PendingTransaction }> {
    const stored = this.rootStore.spaceStore.getById(spaceId)
    if (!stored) throw new Error(`Space ${spaceId} not found`)
    const previous = toJS(stored)

    const merged = { ...previous, ...changes }

    // 1. Optimistic
    this.rootStore.spaceStore.upsert(merged)

    // 2. Persist
    try {
      await this.rootStore.spaceDB!.put(merged)
    } catch {
      this.rootStore.spaceStore.upsert(previous)
      throw new Error('Failed to persist update locally')
    }

    // 3. Enqueue transaction — no network call here
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'U',
      'Space',
      spaceId,
      merged as unknown as Record<string, unknown>,
      previous as unknown as Record<string, unknown>
    )

    return { previous, tx }
  }

  // ── UPDATE (immediate — local write + queue + synchronous send) ──
  async update(spaceId: string, changes: Partial<SpaceRecord>): Promise<void> {
    const { previous, tx } = await this.updateLocal(spaceId, changes)

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.put(`/spaces/sync/${spaceId}`, changes, {
        headers: {
          'X-Workspace-Id': this.rootStore.currentWorkspaceId!,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (axios.isAxiosError(err) && !err.response) {
        console.warn('You are offline. Update will sync when connection is restored.')
        return
      }

      this.rootStore.spaceStore.upsert(previous)
      await this.rootStore.spaceDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }

  // ── DELETE ──
  async delete(spaceId: string): Promise<void> {
    const stored = this.rootStore.spaceStore.getById(spaceId)
    if (!stored) throw new Error(`Space ${spaceId} not found`)
    const previous = toJS(stored)

    // Cancel pending transactions for all children first — they're now invalid
    const childIds = [
      ...this.rootStore.folderStore.all.filter(f => f.spaceId === spaceId).map(f => f.id),
      ...this.rootStore.taskStore.all.filter(t => t.spaceId === spaceId).map(t => t.id),
    ]
    for (const id of childIds) {
      await this.syncEngine.transactionQueue.cancelByEntityId(id)
    }

    // Cascade eager removal of children
    for (const f of this.rootStore.folderStore.all.filter(f => f.spaceId === spaceId)) {
      this.rootStore.folderStore.remove(f.id)
      await this.rootStore.folderDB!.delete(f.id)
    }
    for (const t of this.rootStore.taskStore.all.filter(t => t.spaceId === spaceId)) {
      this.rootStore.taskStore.remove(t.id)
      await this.rootStore.taskDB!.delete(t.id)
    }
    for (const s of this.rootStore.statusStore.all.filter((s: { spaceId: string }) => s.spaceId === spaceId)) {
      this.rootStore.statusStore.remove(s.id)
    }

    // 1. Eager local removal of space itself
    this.rootStore.spaceStore.remove(spaceId)

    // 2. Persist
    try {
      await this.rootStore.spaceDB!.delete(spaceId)
    } catch {
      this.rootStore.spaceStore.upsert(previous)
      throw new Error('Failed to persist delete locally')
    }

    // 3. Enqueue
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'D',
      'Space',
      spaceId,
      { id: spaceId },
      previous as unknown as Record<string, unknown>
    )

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.delete(`/spaces/sync/${spaceId}`, {
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

      this.rootStore.spaceStore.upsert(previous)
      await this.rootStore.spaceDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }
}
