import type { IDBPDatabase } from "idb";
import type { TaskPlanDB } from "../schema";
import type { FavoriteRecord } from "@/types/projects/favorite-record";

export class FavoriteDB {
  private db: IDBPDatabase<TaskPlanDB>

  constructor(db: IDBPDatabase<TaskPlanDB>) {
    this.db = db
  }

  async get(entityId: string): Promise<FavoriteRecord | undefined> {
    return this.db.get('favorites', entityId)
  }

  async getAll(): Promise<FavoriteRecord[]> {
    return this.db.getAll('favorites')
  }

  async put(favorite: FavoriteRecord): Promise<void> {
    await this.db.put('favorites', favorite)
  }

  async putMany(favorites: FavoriteRecord[]): Promise<void> {
    const tx = this.db.transaction('favorites', 'readwrite')
    await Promise.all([
      ...favorites.map((f) => tx.store.put(f)),
      tx.done,
    ])
  }

  async delete(entityId: string): Promise<void> {
    await this.db.delete('favorites', entityId)
  }

  async clear(): Promise<void> {
    await this.db.clear('favorites')
  }
}
