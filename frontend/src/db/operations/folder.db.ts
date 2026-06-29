import type { IDBPDatabase } from "idb";
import type { TaskPlanDB } from "../schema";
import type { FolderRecord } from "@/types/projects/folder-record";

export class FolderDB {
  private db: IDBPDatabase<TaskPlanDB>

  constructor(db: IDBPDatabase<TaskPlanDB>) {
    this.db = db
  }

  async get(id: string): Promise<FolderRecord | undefined> {
    return this.db.get('folders', id)
  }

  async getAll(): Promise<FolderRecord[]> {
    return this.db.getAll('folders')
  }

  async getAllBySpace(spaceId: string): Promise<FolderRecord[]> {
    return this.db.getAllFromIndex('folders', 'by-space', spaceId)
  }

  async put(folder: FolderRecord): Promise<void> {
    await this.db.put('folders', folder)
  }

  async putMany(folders: FolderRecord[]): Promise<void> {
    const tx = this.db.transaction('folders', 'readwrite')
    await Promise.all([
      ...folders.map((f) => tx.store.put(f)),
      tx.done,
    ])
  }

  async delete(id: string): Promise<void> {
    await this.db.delete('folders', id)
  }

  async clear(): Promise<void> {
    await this.db.clear('folders')
  }
}
