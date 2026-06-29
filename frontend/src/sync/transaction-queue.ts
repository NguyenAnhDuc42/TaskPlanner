import type { EntityType, SyncAction } from '@/types/sync/delta'
import type { PendingTransaction } from '@/types/sync/transaction'
import type { RootStore } from '@/stores/root.store'

export class TransactionQueue {
  private rootStore: RootStore
  private flushing = false

  constructor(rootStore: RootStore) {
    this.rootStore = rootStore
  }

  async enqueue(
    type: SyncAction,
    entityType: EntityType,
    entityId: string,
    data: Record<string, unknown>,
    previousData: Record<string, unknown> | null
  ): Promise<PendingTransaction> {
    const tx: PendingTransaction = {
      id: crypto.randomUUID(),
      action:type,
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

  /**
   * Flush all pending transactions to the server.
   * Called on reconnect or after network recovery.
   */
  async flush(sendToServer: (tx: PendingTransaction) => Promise<void>): Promise<void> {
    if (this.flushing) return
    this.flushing = true

    try {
      const pending = await this.rootStore.transactionDB!.getPending()

      // Process in order (oldest first)
      const sorted = pending.sort((a, b) => a.createdAt - b.createdAt)

      for (const tx of sorted) {
        await this.rootStore.transactionDB!.markInFlight(tx.id)

        try {
          await sendToServer(tx)
          // Don't dequeue here — delta handler dequeues on SignalR confirmation
        } catch {
          // Server rejected, mark back as pending for retry
          await this.rootStore.transactionDB!.markPending(tx.id)
          break
        }
      }
    } finally {
      this.flushing = false
    }
  }

  /**
   * Recover in-flight transactions that were interrupted (e.g., tab closed).
   * Called on app startup.
   */
  async recoverInFlight(): Promise<void> {
    const inFlight = await this.rootStore.transactionDB!.getInFlight()
    for (const tx of inFlight) {
      // Mark back as pending so flush() picks them up
      await this.rootStore.transactionDB!.markPending(tx.id)
    }
  }
}