import type { FavoriteRecord } from "@/types/projects/favorite-record";
import { makeAutoObservable, observable } from "mobx";

export class FavoriteStore {
  // deep: false — records replaced wholesale, never mutated in place. See DocumentBlockStore.
  favorites = observable.map<string, FavoriteRecord>({}, { deep: false });

  constructor() {
    makeAutoObservable(this);
  }

  get all(): FavoriteRecord[] {
    return Array.from(this.favorites.values());
  }

  getByEntityId(entityId: string): FavoriteRecord | undefined {
    return this.favorites.get(entityId);
  }

  isFavorite(entityId: string): boolean {
    return this.favorites.has(entityId);
  }

  upsert(favorite: FavoriteRecord): void {
    this.favorites.set(favorite.entityId, favorite);
  }

  remove(entityId: string): void {
    this.favorites.delete(entityId);
  }

  hydrate(favorites: FavoriteRecord[]): void {
    this.favorites.clear();
    for (const f of favorites) this.favorites.set(f.entityId, f);
  }

  clear(): void {
    this.favorites.clear();
  }
}
