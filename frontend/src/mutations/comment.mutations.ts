import type { WorkspaceRootStore } from '@/stores/workspace-root.store'
import { getActiveRootStore } from '@/stores/root.store'
import type { SyncEngine } from '@/sync/sync-engine'
import type { CommentRecord } from '@/types/projects/comment-record'
import { api } from '@/lib/api-client'
import { isConnectivityError, isNotFoundError } from "@/lib/is-connectivity-error";
import { devError } from '@/sync/dev-log'
import { toJS } from 'mobx'
import { toast } from 'sonner'

export class CommentMutations {
  private rootStore: WorkspaceRootStore
  private syncEngine: SyncEngine
  private currentUserId: string | null

  constructor(rootStore: WorkspaceRootStore, syncEngine: SyncEngine, currentUserId: string | null) {
    this.rootStore = rootStore
    this.syncEngine = syncEngine
    this.currentUserId = currentUserId
  }

  async create(data: { taskId: string; content: string; parentCommentId?: string | null }): Promise<CommentRecord> {
    const id = crypto.randomUUID()
    const creatorId = this.rootStore.memberStore.getByUserId(this.currentUserId ?? '')?.id ?? ''

    const record: CommentRecord = {
      id,
      content: data.content,
      creatorId,
      taskId: data.taskId,
      parentCommentId: data.parentCommentId ?? undefined,
      isEdited: false,
      createdAt: new Date().toISOString(),
    }

    this.rootStore.commentStore.upsert(record)

    try {
      await this.rootStore.commentDB!.put(record)
    } catch (err) {
      this.rootStore.commentStore.remove(record.id)
      devError('[CommentMutations] commentDB.put failed:', err)
      toast.error('Failed to save comment locally. Please try again.')
      throw new Error(`Failed to persist comment locally: ${err instanceof Error ? err.message : String(err)}`)
    }

    const payload = {
      id,
      projectTaskId: data.taskId,
      content: data.content,
      parentCommentId: data.parentCommentId ?? null,
    }

    const tx = await this.syncEngine.transactionQueue.enqueue('C', 'Comment', record.id, payload, null)

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return record
    }

    try {
      await api.post('/comments/sync', payload, {
        headers: {
          'X-Workspace-Id': this.rootStore.workspaceId,
          'X-Client-Trace-Id': tx.id,
        }
      })
    } catch (err) {
      if (isConnectivityError(err)) {
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

  async update(commentId: string, content: string): Promise<void> {
    const stored = this.rootStore.commentStore.getById(commentId)
    if (!stored) throw new Error(`Comment ${commentId} not found`)
    const previous = toJS(stored)

    const merged: CommentRecord = { ...previous, content, isEdited: true }

    this.rootStore.commentStore.upsert(merged)

    try {
      await this.rootStore.commentDB!.put(merged)
    } catch {
      this.rootStore.commentStore.upsert(previous)
      toast.error('Failed to save comment locally. Please try again.')
      throw new Error('Failed to persist update locally')
    }

    const tx = await this.syncEngine.transactionQueue.enqueue(
      'U',
      'Comment',
      commentId,
      merged as unknown as Record<string, unknown>,
      previous as unknown as Record<string, unknown>
    )

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.put(`/comments/sync/${commentId}`, { content }, {
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

      this.rootStore.commentStore.upsert(previous)
      await this.rootStore.commentDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }

  async delete(commentId: string): Promise<void> {
    const stored = this.rootStore.commentStore.getById(commentId)
    if (!stored) throw new Error(`Comment ${commentId} not found`)
    const previous = toJS(stored)

    this.rootStore.commentStore.remove(commentId)

    try {
      await this.rootStore.commentDB!.delete(commentId)
    } catch {
      this.rootStore.commentStore.upsert(previous)
      toast.error('Failed to delete comment locally. Please try again.')
      throw new Error('Failed to persist delete locally')
    }

    const tx = await this.syncEngine.transactionQueue.enqueue(
      'D',
      'Comment',
      commentId,
      { id: commentId },
      previous as unknown as Record<string, unknown>
    )

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn('App is offline. Skipping API request. Will sync later.')
      return
    }

    try {
      await api.delete(`/comments/sync/${commentId}`, {
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

      this.rootStore.commentStore.upsert(previous)
      await this.rootStore.commentDB!.put(previous)
      await this.syncEngine.transactionQueue.dequeue(tx.id)
      throw err
    }
  }
}
