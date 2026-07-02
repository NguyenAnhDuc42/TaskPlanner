import type { RootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { DocumentBlockRecord } from '@/types/document/document-block-record'
import type { BlockType } from '@/types/block-type'
import type { PendingTransaction } from '@/types/sync/transaction'
import { api } from '@/lib/api-client'
import { devError } from '@/sync/dev-log'
import axios from 'axios'
import { toJS } from 'mobx'

// A single block-editor "save" can create, update, AND delete several blocks at once (typed in
// block 3, added block 7, removed block 2). Each block is its own entity — that's 3 separate
// SyncEvents, not one merged event — but they should all reach the server in ONE HTTP call.
// The *Local() methods below do only the local part (store/IndexedDB/enqueue, no network) so a
// caller can queue up N block changes, then trigger a single shared flush (TransactionQueue
// already sends everything pending in one POST /api/sync/batch call — squash() only merges
// transactions that share the same entityId, so N different blocks stay as N separate items).
// The non-Local methods keep the old immediate-send behavior for single standalone block actions.
export class DocumentBlockMutations {
  private rootStore: RootStore
  private syncEngine: SyncEngine

  constructor(rootStore: RootStore, syncEngine: SyncEngine) {
    this.rootStore = rootStore
    this.syncEngine = syncEngine
  }

  // ── CREATE (local-only) ──
  async createLocal(data: { id?: string; documentId: string; type: BlockType; content: string; orderKey: string }): Promise<{ record: DocumentBlockRecord; tx: PendingTransaction }> {
    const id = data.id ?? crypto.randomUUID()

    const record: DocumentBlockRecord = {
      id,
      documentId: data.documentId,
      type: data.type,
      content: data.content,
      orderKey: data.orderKey,
    }

    // 1. Optimistic
    this.rootStore.documentBlockStore.upsert(record)

    // 2. Persist
    try {
      await this.rootStore.documentBlockDB!.put(record)
    } catch (err) {
      this.rootStore.documentBlockStore.remove(record.id)
      devError('[DocumentBlockMutations] documentBlockDB.put failed:', err)
      throw new Error(`Failed to persist document block locally: ${err instanceof Error ? err.message : String(err)}`)
    }

    // CreateDocumentBlockCommand wire shape
    const commandPayload = {
      id,
      documentId: data.documentId,
      type: data.type,
      content: data.content,
      orderKey: data.orderKey,
    }

    // 3. Enqueue transaction — no network call here
    const tx = await this.syncEngine.transactionQueue.enqueue('C', 'DocumentBlock', record.id, commandPayload, null)

    return { record, tx }
  }

  // ── CREATE (immediate) ──
  async create(data: { documentId: string; type: BlockType; content: string; orderKey: string }): Promise<DocumentBlockRecord> {
    const { record, tx } = await this.createLocal(data)

    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return record
    }

    try {
      await api.post('/document-blocks/sync', tx.data, {
        headers: {
          'X-Workspace-Id': this.rootStore.currentWorkspaceId!,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (axios.isAxiosError(err) && !err.response) {
        console.warn('You are offline. Block will sync when connection is restored.')
        return record
      }

      this.rootStore.documentBlockStore.remove(record.id)
      await this.rootStore.documentBlockDB!.delete(record.id)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }

    return record
  }

  // ── UPDATE (local-only) ──
  async updateLocal(blockId: string, changes: Partial<Pick<DocumentBlockRecord, 'content' | 'orderKey' | 'type'>>): Promise<{ previous: DocumentBlockRecord; tx: PendingTransaction }> {
    const stored = this.rootStore.documentBlockStore.getById(blockId)
    if (!stored) throw new Error(`Document block ${blockId} not found`)
    const previous = toJS(stored)

    const merged = { ...previous, ...changes }

    // 1. Optimistic
    this.rootStore.documentBlockStore.upsert(merged)

    // 2. Persist
    try {
      await this.rootStore.documentBlockDB!.put(merged)
    } catch {
      this.rootStore.documentBlockStore.upsert(previous)
      throw new Error('Failed to persist update locally')
    }

    // 3. Enqueue — no network call here
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'U',
      'DocumentBlock',
      blockId,
      changes as unknown as Record<string, unknown>,
      previous as unknown as Record<string, unknown>
    )

    return { previous, tx }
  }

  // ── UPDATE (immediate) ──
  async update(blockId: string, changes: Partial<Pick<DocumentBlockRecord, 'content' | 'orderKey' | 'type'>>): Promise<void> {
    const { previous, tx } = await this.updateLocal(blockId, changes)

    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.put(`/document-blocks/sync/${blockId}`, tx.data, {
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

      this.rootStore.documentBlockStore.upsert(previous)
      await this.rootStore.documentBlockDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }

  // ── DELETE (local-only) ──
  async deleteLocal(blockId: string): Promise<{ previous: DocumentBlockRecord; tx: PendingTransaction }> {
    const stored = this.rootStore.documentBlockStore.getById(blockId)
    if (!stored) throw new Error(`Document block ${blockId} not found`)
    const previous = toJS(stored)

    // 1. Eager local removal
    this.rootStore.documentBlockStore.remove(blockId)

    // 2. Persist
    try {
      await this.rootStore.documentBlockDB!.delete(blockId)
    } catch {
      this.rootStore.documentBlockStore.upsert(previous)
      throw new Error('Failed to persist delete locally')
    }

    // 3. Enqueue — no network call here
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'D',
      'DocumentBlock',
      blockId,
      { id: blockId },
      previous as unknown as Record<string, unknown>
    )

    return { previous, tx }
  }

  // ── DELETE (immediate) ──
  async delete(blockId: string): Promise<void> {
    const { previous, tx } = await this.deleteLocal(blockId)

    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.delete(`/document-blocks/sync/${blockId}`, {
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

      this.rootStore.documentBlockStore.upsert(previous)
      await this.rootStore.documentBlockDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }
}
