import type { RootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { DocumentBlockRecord } from '@/types/document/document-block-record'
import type { BlockType } from '@/types/block-type'
import { api } from '@/lib/api-client'
import { devError } from '@/sync/dev-log'
import axios from 'axios'
import { toJS } from 'mobx'

export class DocumentBlockMutations {
  private rootStore: RootStore
  private syncEngine: SyncEngine

  constructor(rootStore: RootStore, syncEngine: SyncEngine) {
    this.rootStore = rootStore
    this.syncEngine = syncEngine
  }

  // ── CREATE ──
  async create(data: { documentId: string; type: BlockType; content: string; orderKey: string }): Promise<DocumentBlockRecord> {
    const id = crypto.randomUUID()

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

    // 3. Enqueue transaction
    const tx = await this.syncEngine.transactionQueue.enqueue('C', 'DocumentBlock', record.id, commandPayload, null)

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return record
    }

    try {
      await api.post('/document-blocks/sync', commandPayload, {
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

  // ── UPDATE ──
  async update(blockId: string, changes: Partial<Pick<DocumentBlockRecord, 'content' | 'orderKey' | 'type'>>): Promise<void> {
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

    // 3. Enqueue
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'U',
      'DocumentBlock',
      blockId,
      merged as unknown as Record<string, unknown>,
      previous as unknown as Record<string, unknown>
    )

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.put(`/document-blocks/sync/${blockId}`, changes, {
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

  // ── DELETE ──
  async delete(blockId: string): Promise<void> {
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

    // 3. Enqueue
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'D',
      'DocumentBlock',
      blockId,
      { id: blockId },
      previous as unknown as Record<string, unknown>
    )

    // 4. Synchronous API call
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
