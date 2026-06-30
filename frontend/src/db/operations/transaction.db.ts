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
 
  async markPending(id: string): Promise<void> {
    const tx = await this.db.get('__transactions', id)
    if (tx) {
      tx.status = 'pending'
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