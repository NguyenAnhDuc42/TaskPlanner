import type { IDBPDatabase } from "idb";
import type { TaskPlanDB } from "../schema";
import type { Status } from "@/types/status";

export class StatusDB {
  private db: IDBPDatabase<TaskPlanDB>

  constructor(db: IDBPDatabase<TaskPlanDB>) {
    this.db = db
  }

  async get(id: string): Promise<Status | undefined> {
    return this.db.get('statuses', id)
  }

  async getAll(): Promise<Status[]> {
    return this.db.getAll('statuses')
  }

  async getAllBySpace(spaceId: string): Promise<Status[]> {
    return this.db.getAllFromIndex('statuses', 'by-space', spaceId)
  }

  async put(status: Status): Promise<void> {
    await this.db.put('statuses', status)
  }

  async putMany(statuses: Status[]): Promise<void> {
    const tx = this.db.transaction('statuses', 'readwrite')
    await Promise.all([
      ...statuses.map((s) => tx.store.put(s)),
      tx.done,
    ])
  }

  async delete(id: string): Promise<void> {
    await this.db.delete('statuses', id)
  }

  async clear(): Promise<void> {
    await this.db.clear('statuses')
  }
}
