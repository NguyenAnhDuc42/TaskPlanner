import type { IDBPDatabase } from "idb";
import type { TaskPlanDB } from "../schema";
import type { EntityAccessRecord } from "@/types/workspace/entity-access-record";

export class EntityAccessDB {
  private db: IDBPDatabase<TaskPlanDB>

  constructor(db: IDBPDatabase<TaskPlanDB>) {
    this.db = db
  }

  async get(id: string): Promise<EntityAccessRecord | undefined> {
    return this.db.get('entity_access', id)
  }

  async getAll(): Promise<EntityAccessRecord[]> {
    return this.db.getAll('entity_access')
  }

  async getAllBySpace(spaceId: string): Promise<EntityAccessRecord[]> {
    return this.db.getAllFromIndex('entity_access', 'by-space', spaceId)
  }

  async put(record: EntityAccessRecord): Promise<void> {
    await this.db.put('entity_access', record)
  }

  async putMany(records: EntityAccessRecord[]): Promise<void> {
    const tx = this.db.transaction('entity_access', 'readwrite')
    await Promise.all([
      ...records.map((r) => tx.store.put(r)),
      tx.done,
    ])
  }

  async delete(id: string): Promise<void> {
    await this.db.delete('entity_access', id)
  }

  async clear(): Promise<void> {
    await this.db.clear('entity_access')
  }
}
