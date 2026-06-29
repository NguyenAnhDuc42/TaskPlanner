import type { IDBPDatabase } from "idb";
import type { TaskPlanDB } from "../schema";
import type { SpaceRecord } from "@/types/projects/space-record";

export class SpaceDB {
  private db: IDBPDatabase<TaskPlanDB>

  constructor(db: IDBPDatabase<TaskPlanDB>) {
    this.db = db
  }

  async get(id: string): Promise<SpaceRecord | undefined> {
    return this.db.get('spaces', id)
  }

  async getAll(): Promise<SpaceRecord[]> {
    return this.db.getAll('spaces')
  }

  async put(space: SpaceRecord): Promise<void> {
    await this.db.put('spaces', space)
  }

  async putMany(spaces: SpaceRecord[]): Promise<void> {
    const tx = this.db.transaction('spaces', 'readwrite')
    await Promise.all([
      ...spaces.map((s) => tx.store.put(s)),
      tx.done,
    ])
  }

  async delete(id: string): Promise<void> {
    await this.db.delete('spaces', id)
  }

  async clear(): Promise<void> {
    await this.db.clear('spaces')
  }
}
