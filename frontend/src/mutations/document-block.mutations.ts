import type { WorkspaceRootStore } from '@/stores/workspace-root.store'
import { getActiveRootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { DocumentBlockRecord } from '@/types/document/document-block-record'
import type { BlockType } from '@/types/block-type'
import type { PendingTransaction } from '@/types/sync/transaction'
import { api } from '@/lib/api-client'
import { isConnectivityError, isNotFoundError } from "@/lib/is-connectivity-error";
import { devError } from '@/sync/dev-log'
import { toJS } from 'mobx'
import { toast } from 'sonner'

export class DocumentBlockMutations {
  private rootStore: WorkspaceRootStore
  private syncEngine: SyncEngine

  constructor(rootStore: WorkspaceRootStore, syncEngine: SyncEngine) {
    this.rootStore = rootStore
    this.syncEngine = syncEngine
  }

  async createLocal(data: { id?: string; documentId: string; type: BlockType; content: string; orderKey: string }): Promise<{ record: DocumentBlockRecord; tx: PendingTransaction }> {
    const id = data.id ?? crypto.randomUUID()

    const record: DocumentBlockRecord = {
      id,
      documentId: data.documentId,
      type: data.type,
      content: data.content,
      orderKey: data.orderKey,
    }

    this.rootStore.documentBlockStore.upsert(record)

    try {
      await this.rootStore.documentBlockDB!.put(record)
    } catch (err) {
      this.rootStore.documentBlockStore.remove(record.id)
      devError('[DocumentBlockMutations] documentBlockDB.put failed:', err)
      toast.error('Failed to save block locally. Please try again.')
      throw new Error(`Failed to persist document block locally: ${err instanceof Error ? err.message : String(err)}`)
    }

    const payload = {
      id,
      documentId: data.documentId,
      type: data.type,
      content: data.content,
      orderKey: data.orderKey,
    }

    const tx = await this.syncEngine.transactionQueue.enqueue('C', 'DocumentBlock', record.id, payload, null)

    return { record, tx }
  }

  async create(data: { documentId: string; type: BlockType; content: string; orderKey: string }): Promise<DocumentBlockRecord> {
    const { record, tx } = await this.createLocal(data)

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return record
    }

    try {
      await api.post('/document-blocks/sync', tx.data, {
        headers: {
          'X-Workspace-Id': this.rootStore.workspaceId,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (isConnectivityError(err)) {
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

  async updateLocal(blockId: string, changes: Partial<Pick<DocumentBlockRecord, 'content' | 'orderKey' | 'type'>>): Promise<{ previous: DocumentBlockRecord; tx: PendingTransaction }> {
    const stored = this.rootStore.documentBlockStore.getById(blockId)
    if (!stored) throw new Error(`Document block ${blockId} not found`)
    const previous = toJS(stored)

    const merged = { ...previous, ...changes }

    this.rootStore.documentBlockStore.upsert(merged)

    try {
      await this.rootStore.documentBlockDB!.put(merged)
    } catch {
      this.rootStore.documentBlockStore.upsert(previous)
      toast.error('Failed to save block locally. Please try again.')
      throw new Error('Failed to persist update locally')
    }

    const tx = await this.syncEngine.transactionQueue.enqueue(
      'U',
      'DocumentBlock',
      blockId,
      changes as unknown as Record<string, unknown>,
      previous as unknown as Record<string, unknown>
    )

    return { previous, tx }
  }

  async update(blockId: string, changes: Partial<Pick<DocumentBlockRecord, 'content' | 'orderKey' | 'type'>>): Promise<void> {
    const { previous, tx } = await this.updateLocal(blockId, changes)

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.put(`/document-blocks/sync/${blockId}`, tx.data, {
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

      this.rootStore.documentBlockStore.upsert(previous)
      await this.rootStore.documentBlockDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }

  async deleteLocal(blockId: string): Promise<{ previous: DocumentBlockRecord; tx: PendingTransaction }> {
    const stored = this.rootStore.documentBlockStore.getById(blockId)
    if (!stored) throw new Error(`Document block ${blockId} not found`)
    const previous = toJS(stored)

    this.rootStore.documentBlockStore.remove(blockId)

    try {
      await this.rootStore.documentBlockDB!.delete(blockId)
    } catch {
      this.rootStore.documentBlockStore.upsert(previous)
      toast.error('Failed to delete block locally. Please try again.')
      throw new Error('Failed to persist delete locally')
    }

    const tx = await this.syncEngine.transactionQueue.enqueue(
      'D',
      'DocumentBlock',
      blockId,
      { id: blockId },
      previous as unknown as Record<string, unknown>
    )

    return { previous, tx }
  }

  async delete(blockId: string): Promise<void> {
    const { previous, tx } = await this.deleteLocal(blockId)

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.delete(`/document-blocks/sync/${blockId}`, {
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

      this.rootStore.documentBlockStore.upsert(previous)
      await this.rootStore.documentBlockDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }
}
