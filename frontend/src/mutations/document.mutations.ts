import type { RootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { DocumentRecord } from '@/types/document/document-record'
import { api } from '@/lib/api-client'
import { isConnectivityError, isNotFoundError } from "@/lib/is-connectivity-error";
import { toJS } from 'mobx'

// No create() here — Document is only ever created as a side-effect of Task/Space
// creation (see CreateTaskHandler/CreateSpaceHandler on the backend), never standalone.
export class DocumentMutations {
  private rootStore: RootStore
  private syncEngine: SyncEngine

  constructor(rootStore: RootStore, syncEngine: SyncEngine) {
    this.rootStore = rootStore
    this.syncEngine = syncEngine
  }

  // ── UPDATE (rename) ──
  async update(documentId: string, name: string): Promise<void> {
    const stored = this.rootStore.documentStore.getById(documentId)
    if (!stored) throw new Error(`Document ${documentId} not found`)
    const previous = toJS(stored)

    const merged: DocumentRecord = { ...previous, name }

    // 1. Optimistic
    this.rootStore.documentStore.upsert(merged)

    // 2. Persist
    try {
      await this.rootStore.documentDB!.put(merged)
    } catch {
      this.rootStore.documentStore.upsert(previous)
      throw new Error('Failed to persist update locally')
    }

    // 3. Enqueue
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'U',
      'Document',
      documentId,
      merged as unknown as Record<string, unknown>,
      previous as unknown as Record<string, unknown>
    )

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.put(`/documents/sync/${documentId}`, { name }, {
        headers: {
          'X-Workspace-Id': this.rootStore.currentWorkspaceId!,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (isConnectivityError(err)) {
        console.warn('You are offline. Update will sync when connection is restored.')
        return
      }

      this.rootStore.documentStore.upsert(previous)
      await this.rootStore.documentDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }

  // ── DELETE ──
  async delete(documentId: string): Promise<void> {
    const stored = this.rootStore.documentStore.getById(documentId)
    if (!stored) throw new Error(`Document ${documentId} not found`)
    const previous = toJS(stored)

    // 1. Eager local removal
    this.rootStore.documentStore.remove(documentId)

    // 2. Persist
    try {
      await this.rootStore.documentDB!.delete(documentId)
    } catch {
      this.rootStore.documentStore.upsert(previous)
      throw new Error('Failed to persist delete locally')
    }

    // 3. Enqueue
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'D',
      'Document',
      documentId,
      { id: documentId },
      previous as unknown as Record<string, unknown>
    )

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.delete(`/documents/sync/${documentId}`, {
        headers: {
          'X-Workspace-Id': this.rootStore.currentWorkspaceId!,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (isConnectivityError(err)) {
        console.warn('You are offline. Deletion will sync when connection is restored.')
        return
      }

      if (isNotFoundError(err)) {
        // Already deleted server-side (a retried/duplicate delete, or another client beat us to
        // it) — the desired end state is already correct, don't resurrect it locally.
        await this.syncEngine.transactionQueue.dequeue(tx.id)
        return
      }

      this.rootStore.documentStore.upsert(previous)
      await this.rootStore.documentDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }
}
