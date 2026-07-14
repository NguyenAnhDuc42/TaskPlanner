import type { SyncEntityType, SyncAction } from '@/types/sync/delta'
import type { PendingTransaction } from '@/types/sync/transaction'
import type { WorkspaceRootStore } from '@/stores/workspace-root.store'

export class TransactionQueue {
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

  async dequeue(txId: string): Promise<void> {
    await this.rootStore.transactionDB!.dequeue(txId)
  }

  // Cancel all pending AND in-flight transactions for a given entity.
  // In-flight txs can't be un-sent but dequeuing prevents them from being retried,
  // and the applyDelta parent-space guard prevents their echo from re-adding the entity.
  async cancelByEntityId(entityId: string): Promise<void> {
    await this.cancelByEntityIds([entityId])
  }

  // Bulk variant for cascade deletes: one pending+inFlight read for the whole id set, instead of
  // two IDB getAll's per child entity (a space with N children used to cost 2N reads).
  async cancelByEntityIds(entityIds: string[]): Promise<void> {
    if (entityIds.length === 0) return
    const ids = new Set(entityIds)
    const [pending, inFlight] = await Promise.all([
      this.rootStore.transactionDB!.getPending(),
      this.rootStore.transactionDB!.getInFlight(),
    ])
    for (const tx of [...pending, ...inFlight].filter(t => ids.has(t.entityId))) {
      await this.rootStore.transactionDB!.dequeue(tx.id)
    }
  }

  // Collapse redundant transactions for the same entity before sending.
  // Rules (applied per-entityId group, oldest-first):
  //   C+D  → cancel both (never reached server)
  //   D    → cancel all preceding Us, keep D
  //   C+U(s) → merge Us into C, send one C
  //   U+U(s) → merge into one U (last-write-wins per field)
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
        // C+D: entity never reached server → cancel everything
        toCancel.push(...txs.map(t => t.id))
      } else if (del) {
        // D beats all pending Us
        toCancel.push(...updates.map(t => t.id))
        toSend.push(del)
      } else if (create && updates.length > 0) {
        // C+U(s): merge all updates into the create payload
        const mergedData = updates.reduce((acc, u) => ({ ...acc, ...u.data }), { ...create.data })
        toCancel.push(...updates.map(t => t.id))
        toSend.push({ ...create, data: mergedData })
      } else if (!create && updates.length > 1) {
        // U+U(s): merge into one U
        const mergedData = updates.reduce((acc, u) => ({ ...acc, ...u.data }), {} as Record<string, unknown>)
        toCancel.push(...updates.slice(0, -1).map(t => t.id))
        toSend.push({ ...updates[updates.length - 1], data: mergedData })
      } else {
        toSend.push(...txs)
      }
    }

    // Preserve causal order by first-occurrence time of each entity group
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

      for (const id of toCancel) {
        await this.rootStore.transactionDB!.dequeue(id)
      }

      if (toSend.length === 0) return

      for (const tx of toSend) {
        await this.rootStore.transactionDB!.markInFlight(tx.id)
      }

      try {
        const results = await sendBatch(toSend)
        // Mark failed items back to pending so they retry next flush.
        // Successful items are dequeued by the SignalR DeltaBatch (clientTraceId matching).
        for (const r of results) {
          if (!r.success) {
            await this.rootStore.transactionDB!.markPending(r.traceId)
          }
        }
      } catch {
        // Network error — mark all back to pending
        for (const tx of toSend) {
          await this.rootStore.transactionDB!.markPending(tx.id)
        }
      }
    } finally {
      this.flushing = false
    }
  }

  async recoverInFlight(): Promise<void> {
    const inFlight = await this.rootStore.transactionDB!.getInFlight()
    for (const tx of inFlight) {
      await this.rootStore.transactionDB!.markPending(tx.id)
    }
  }
}
