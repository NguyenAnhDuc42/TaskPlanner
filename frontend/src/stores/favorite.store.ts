import type { FavoriteRecord } from "@/types/projects/favorite-record";
import { makeAutoObservable, observable } from "mobx";

// One favorite lives in one place — keyed by entityId, not duplicated onto TaskRecord/
// FolderRecord/SpaceRecord. Mirrors the backend's own `favorites` table shape (see
// GetBootstrapHandler.favoritesSql). This is what fixed the whole class of bugs where an
// unrelated Task/Folder/Space update silently wiped favorite state: it's no longer a field on
// those records at all, so nothing about their sync path can touch it.
export class FavoriteStore {
  favorites = observable.map<string, FavoriteRecord>();

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
