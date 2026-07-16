import type { IDBPDatabase } from "idb";
import type { TaskPlanDB } from "../schema";
import type { PendingTransaction } from "@/types/sync";

export class TransactionDB {
  private db: IDBPDatabase<TaskPlanDB>

  constructor(db: IDBPDatabase<TaskPlanDB>) {
    this.db = db
  }

  async enqueue(tx: PendingTransaction): Promise<void> {
    await this.db.put('__transactions', tx)
  }

  // One readwrite transaction for the whole batch — a huge paste enqueues hundreds of
  // transactions, and one-IDB-transaction-per-enqueue is both slow and partial-failure-prone.
  async enqueueMany(txs: PendingTransaction[]): Promise<void> {
    if (txs.length === 0) return
    const dbTx = this.db.transaction('__transactions', 'readwrite')
    await Promise.all([
      ...txs.map((t) => dbTx.store.put(t)),
      dbTx.done,
    ])
  }
 
  async dequeue(id: string): Promise<void> {
    await this.db.delete('__transactions', id)
  }
 
  async getPending(): Promise<PendingTransaction[]> {
    return this.db.getAllFromIndex('__transactions', 'by-status', 'pending')
  }
 
  async getInFlight(): Promise<PendingTransaction[]> {
    return this.db.getAllFromIndex('__transactions', 'by-status', 'in_flight')
  }
 
  async markInFlight(id: string): Promise<void> {
    const tx = await this.db.get('__transactions', id)
    if (tx) {
      tx.status = 'in_flight'
      await this.db.put('__transactions', tx)
    }
  }
 
  async markPending(id: string, retryCount?: number): Promise<void> {
    const tx = await this.db.get('__transactions', id)
    if (tx) {
      tx.status = 'pending'
      if (retryCount !== undefined) tx.retryCount = retryCount
      await this.db.put('__transactions', tx)
    }
  }
 
  async getByEntity(
    entityType: string,
    entityId: string
  ): Promise<PendingTransaction[]> {
    return this.db.getAllFromIndex('__transactions', 'by-entity', [
      entityType,
      entityId,
    ])
  }
 
  async clear(): Promise<void> {
    await this.db.clear('__transactions')
  }
}