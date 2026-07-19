import type { SyncEntityType, SyncAction } from '@/types/sync/delta'
import type { PendingTransaction } from '@/types/sync/transaction'
import type { WorkspaceRootStore } from '@/stores/workspace-root.store'
import { devError } from './dev-log'

export class TransactionQueue {
  private static readonly MAX_SERVER_REJECTIONS = 5

  private rootStore: WorkspaceRootStore
  private flushing = false

  constructor(rootStore: WorkspaceRootStore) {
    this.rootStore = rootStore
  }

  async enqueue(
    type: SyncAction,
    entityType: SyncEntityType,
    entityId: string,
    data: Record<string, unknown>,
    previousData: Record<string, unknown> | null
  ): Promise<PendingTransaction> {
    const tx: PendingTransaction = {
      id: crypto.randomUUID(),
      action: type,
      entityType,
      entityId,
      data,
      previousData,
      createdAt: Date.now(),
      status: 'pending',
      retryCount: 0,
    }

    await this.rootStore.transactionDB!.enqueue(tx)
    return tx
  }

  async enqueueMany(
    items: {
      type: SyncAction
      entityType: SyncEntityType
      entityId: string
      data: Record<string, unknown>
      previousData: Record<string, unknown> | null
    }[],
  ): Promise<PendingTransaction[]> {
    const txs: PendingTransaction[] = items.map((item) => ({
      id: crypto.randomUUID(),
      action: item.type,
      entityType: item.entityType,
      entityId: item.entityId,
      data: item.data,
      previousData: item.previousData,
      createdAt: Date.now(),
      status: 'pending',
      retryCount: 0,
    }))

    await this.rootStore.transactionDB!.enqueueMany(txs)
    return txs
  }

  async dequeue(txId: string): Promise<void> {
    await this.rootStore.transactionDB!.dequeue(txId)
  }

  async cancelByEntityId(entityId: string): Promise<void> {
    await this.cancelByEntityIds([entityId])
  }

  async cancelByEntityIds(entityIds: string[]): Promise<void> {
    if (entityIds.length === 0) return
    const ids = new Set(entityIds)
    const [pending, inFlight] = await Promise.all([
      this.rootStore.transactionDB!.getPending(),
      this.rootStore.transactionDB!.getInFlight(),
    ])
    const toCancel = [...pending, ...inFlight].filter(t => ids.has(t.entityId)).map(t => t.id)
    await this.rootStore.transactionDB!.dequeueMany(toCancel)
  }

  private squash(sorted: PendingTransaction[]): { toSend: PendingTransaction[]; toCancel: string[] } {
    const groups = new Map<string, PendingTransaction[]>()
    const groupFirstTime = new Map<string, number>()

    for (const tx of sorted) {
      if (!groups.has(tx.entityId)) {
        groups.set(tx.entityId, [])
        groupFirstTime.set(tx.entityId, tx.createdAt)
      }
      groups.get(tx.entityId)!.push(tx)
    }

    const toSend: PendingTransaction[] = []
    const toCancel: string[] = []

    for (const [, txs] of groups) {
      const create = txs.find(t => t.action === 'C')
      const del = txs.find(t => t.action === 'D')
      const updates = txs.filter(t => t.action === 'U')

      if (create && del) {
        toCancel.push(...txs.map(t => t.id))
      } else if (del) {
        toCancel.push(...updates.map(t => t.id))
        toSend.push(del)
      } else if (create) {
        const laterTxs = txs.filter(t => t !== create)
        if (laterTxs.length === 0) {
          toSend.push(create)
        } else {
          const mergedData = laterTxs.reduce((acc, t) => ({ ...acc, ...t.data }), { ...create.data })
          toCancel.push(...laterTxs.map(t => t.id))
          toSend.push({ ...create, data: mergedData })
        }
      } else if (updates.length > 1) {
        const mergedData = updates.reduce((acc, u) => ({ ...acc, ...u.data }), {} as Record<string, unknown>)
        toCancel.push(...updates.slice(0, -1).map(t => t.id))
        toSend.push({ ...updates[updates.length - 1], data: mergedData })
      } else {
        toSend.push(...txs)
      }
    }

    toSend.sort((a, b) => (groupFirstTime.get(a.entityId) ?? 0) - (groupFirstTime.get(b.entityId) ?? 0))

    return { toSend, toCancel }
  }

  async flush(sendBatch: (txs: PendingTransaction[]) => Promise<{ traceId: string; success: boolean; error?: string | null }[]>): Promise<void> {
    if (this.flushing) return
    this.flushing = true

    try {
      const pending = await this.rootStore.transactionDB!.getPending()
      if (pending.length === 0) return

      const sorted = pending.sort((a, b) => a.createdAt - b.createdAt)
      const { toSend, toCancel } = this.squash(sorted)

      await this.rootStore.transactionDB!.dequeueMany(toCancel)

      if (toSend.length === 0) return

      await this.rootStore.transactionDB!.markInFlightMany(toSend.map(tx => tx.id))

      try {
        const results = await sendBatch(toSend)
        const toDrop: string[] = []
        const toRetry: { id: string; retryCount: number }[] = []
        for (const r of results) {
          if (!r.success) {
            const tx = toSend.find(t => t.id === r.traceId)
            const rejections = (tx?.retryCount ?? 0) + 1
            if (rejections >= TransactionQueue.MAX_SERVER_REJECTIONS) {
              devError(`[TransactionQueue] dropping poison transaction ${r.traceId} (${tx?.entityType}/${tx?.action}) after ${rejections} server rejections:`, r.error)
              toDrop.push(r.traceId)
            } else {
              toRetry.push({ id: r.traceId, retryCount: rejections })
            }
          }
        }
        await this.rootStore.transactionDB!.dequeueMany(toDrop)
        await this.rootStore.transactionDB!.markPendingMany(toRetry)
      } catch {
        await this.rootStore.transactionDB!.markPendingMany(toSend.map(tx => ({ id: tx.id })))
      }
    } finally {
      this.flushing = false
    }
  }

  async recoverInFlight(): Promise<void> {
    const inFlight = await this.rootStore.transactionDB!.getInFlight()
    await this.rootStore.transactionDB!.markPendingMany(inFlight.map(tx => ({ id: tx.id })))
  }
}
