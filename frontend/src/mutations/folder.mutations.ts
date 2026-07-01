import type { RootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { FolderRecord } from '@/types/projects/folder-record'
import { api } from '@/lib/api-client'
import { devError } from '@/sync/dev-log'
import axios from 'axios'
import { toJS } from 'mobx'

export class FolderMutations {
  private rootStore: RootStore
  private syncEngine: SyncEngine

  constructor(rootStore: RootStore, syncEngine: SyncEngine) {
    this.rootStore = rootStore
    this.syncEngine = syncEngine
  }

  // ── CREATE ──
  async create(data: Omit<FolderRecord, 'id' | 'createdAt'> & { spaceId: string }): Promise<FolderRecord> {
    const id = crypto.randomUUID()

    const record: FolderRecord = { ...data, id, createdAt: new Date().toISOString() }

    // 1. Optimistic
    this.rootStore.folderStore.upsert(record)

    // 2. Persist
    try {
      await this.rootStore.folderDB!.put(record)
    } catch (err) {
      this.rootStore.folderStore.remove(record.id)
      devError('[FolderMutations] folderDB.put failed:', err)
      throw new Error(`Failed to persist folder locally: ${err instanceof Error ? err.message : String(err)}`)
    }

    // CreateFolderCommand wire shape
    const commandPayload = {
      id,
      spaceId: data.spaceId,
      name: data.name,
      color: data.color ?? null,
      icon: data.icon ?? null,
      startDate: data.startDate ?? null,
      dueDate: data.dueDate ?? null,
    }

    // 3. Enqueue transaction
    const tx = await this.syncEngine.transactionQueue.enqueue('C', 'Folder', record.id, commandPayload, null)

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return record
    }

    try {
      await api.post('/folders/sync', commandPayload, {
        headers: {
          'X-Workspace-Id': this.rootStore.currentWorkspaceId!,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (axios.isAxiosError(err) && !err.response) {
        console.warn('You are offline. Folder will sync when connection is restored.')
        return record
      }

      this.rootStore.folderStore.remove(record.id)
      await this.rootStore.folderDB!.delete(record.id)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }

    return record
  }

  // ── UPDATE ──
  async update(folderId: string, changes: Partial<FolderRecord>): Promise<void> {
    const stored = this.rootStore.folderStore.getById(folderId)
    if (!stored) throw new Error(`Folder ${folderId} not found`)
    const previous = toJS(stored)

    const merged = { ...previous, ...changes }

    // 1. Optimistic
    this.rootStore.folderStore.upsert(merged)

    // 2. Persist
    try {
      await this.rootStore.folderDB!.put(merged)
    } catch {
      this.rootStore.folderStore.upsert(previous)
      throw new Error('Failed to persist update locally')
    }

    // 3. Enqueue
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'U',
      'Folder',
      folderId,
      merged as unknown as Record<string, unknown>,
      previous as unknown as Record<string, unknown>
    )

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.put(`/folders/sync/${folderId}`, changes, {
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

      this.rootStore.folderStore.upsert(previous)
      await this.rootStore.folderDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }

  // ── DELETE ──
  async delete(folderId: string): Promise<void> {
    const stored = this.rootStore.folderStore.getById(folderId)
    if (!stored) throw new Error(`Folder ${folderId} not found`)
    const previous = toJS(stored)

    // Reparent tasks in this folder to space level — mirrors what the backend does
    for (const task of this.rootStore.taskStore.all.filter(t => t.folderId === folderId)) {
      const reparented = { ...toJS(task), folderId: null }
      this.rootStore.taskStore.upsert(reparented)
      await this.rootStore.taskDB!.put(reparented)
    }

    // 1. Eager local removal of folder
    this.rootStore.folderStore.remove(folderId)

    // 2. Persist
    try {
      await this.rootStore.folderDB!.delete(folderId)
    } catch {
      this.rootStore.folderStore.upsert(previous)
      throw new Error('Failed to persist delete locally')
    }

    // 3. Enqueue
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'D',
      'Folder',
      folderId,
      { id: folderId },
      previous as unknown as Record<string, unknown>
    )

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.delete(`/folders/sync/${folderId}`, {
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

      this.rootStore.folderStore.upsert(previous)
      await this.rootStore.folderDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }
}
