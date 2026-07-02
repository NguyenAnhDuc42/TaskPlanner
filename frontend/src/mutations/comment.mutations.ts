import type { RootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { CommentRecord } from '@/types/projects/comment-record'
import { api } from '@/lib/api-client'
import { devError } from '@/sync/dev-log'
import axios from 'axios'
import { toJS } from 'mobx'

export class CommentMutations {
  private rootStore: RootStore
  private syncEngine: SyncEngine

  constructor(rootStore: RootStore, syncEngine: SyncEngine) {
    this.rootStore = rootStore
    this.syncEngine = syncEngine
  }

  // ── CREATE ──
  async create(data: { taskId: string; content: string; parentCommentId?: string | null }): Promise<CommentRecord> {
    const id = crypto.randomUUID()
    const creatorId = this.rootStore.memberStore.getByUserId(this.rootStore.currentUserId ?? '')?.id ?? ''

    const record: CommentRecord = {
      id,
      content: data.content,
      creatorId,
      taskId: data.taskId,
      parentCommentId: data.parentCommentId ?? undefined,
      isEdited: false,
      createdAt: new Date().toISOString(),
    }

    // 1. Optimistic
    this.rootStore.commentStore.upsert(record)

    // 2. Persist
    try {
      await this.rootStore.commentDB!.put(record)
    } catch (err) {
      this.rootStore.commentStore.remove(record.id)
      devError('[CommentMutations] commentDB.put failed:', err)
      throw new Error(`Failed to persist comment locally: ${err instanceof Error ? err.message : String(err)}`)
    }

    // CreateCommentCommand wire shape
    const commandPayload = {
      id,
      projectTaskId: data.taskId,
      content: data.content,
      parentCommentId: data.parentCommentId ?? null,
    }

    // 3. Enqueue transaction
    const tx = await this.syncEngine.transactionQueue.enqueue('C', 'Comment', record.id, commandPayload, null)

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return record
    }

    try {
      await api.post('/comments/sync', commandPayload, {
        headers: {
          'X-Workspace-Id': this.rootStore.currentWorkspaceId!,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (axios.isAxiosError(err) && !err.response) {
        console.warn('You are offline. Comment will sync when connection is restored.')
        return record
      }

      this.rootStore.commentStore.remove(record.id)
      await this.rootStore.commentDB!.delete(record.id)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }

    return record
  }

  // ── UPDATE ──
  // Content only — matches UpdateCommentCommand, which is creator-only on the backend.
  async update(commentId: string, content: string): Promise<void> {
    const stored = this.rootStore.commentStore.getById(commentId)
    if (!stored) throw new Error(`Comment ${commentId} not found`)
    const previous = toJS(stored)

    const merged: CommentRecord = { ...previous, content, isEdited: true }

    // 1. Optimistic
    this.rootStore.commentStore.upsert(merged)

    // 2. Persist
    try {
      await this.rootStore.commentDB!.put(merged)
    } catch {
      this.rootStore.commentStore.upsert(previous)
      throw new Error('Failed to persist update locally')
    }

    // 3. Enqueue
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'U',
      'Comment',
      commentId,
      merged as unknown as Record<string, unknown>,
      previous as unknown as Record<string, unknown>
    )

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.put(`/comments/sync/${commentId}`, { content }, {
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

      this.rootStore.commentStore.upsert(previous)
      await this.rootStore.commentDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }

  // ── DELETE ──
  async delete(commentId: string): Promise<void> {
    const stored = this.rootStore.commentStore.getById(commentId)
    if (!stored) throw new Error(`Comment ${commentId} not found`)
    const previous = toJS(stored)

    // 1. Eager local removal
    this.rootStore.commentStore.remove(commentId)

    // 2. Persist
    try {
      await this.rootStore.commentDB!.delete(commentId)
    } catch {
      this.rootStore.commentStore.upsert(previous)
      throw new Error('Failed to persist delete locally')
    }

    // 3. Enqueue
    const tx = await this.syncEngine.transactionQueue.enqueue(
      'D',
      'Comment',
      commentId,
      { id: commentId },
      previous as unknown as Record<string, unknown>
    )

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.delete(`/comments/sync/${commentId}`, {
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

      this.rootStore.commentStore.upsert(previous)
      await this.rootStore.commentDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }
}
