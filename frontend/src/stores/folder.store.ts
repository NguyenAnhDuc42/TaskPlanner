import type { FolderRecord } from "@/types/projects";
import { makeAutoObservable, observable } from "mobx";

export class FolderStore {
  folders = observable.map<string, FolderRecord>();

  constructor() {
    makeAutoObservable(this);
  }

  get all(): FolderRecord[] {
    return Array.from(this.folders.values());
  }

  getById(id: string): FolderRecord | undefined {
    return this.folders.get(id);
  }

  getBySpace(spaceId: string): FolderRecord[] {
    return this.all.filter((f) => f.spaceId === spaceId);
  }

  getFavorites(): FolderRecord[] {
    return this.all
      .filter((f) => f.isFavorite)
      .sort((a, b) => (a.favoriteOrderKey ?? "").localeCompare(b.favoriteOrderKey ?? ""));
  }

  upsert(folder: FolderRecord): void {
    this.folders.set(folder.id, folder);
  }

  upsertMany(folders: FolderRecord[]): void {
    for (const folder of folders) this.folders.set(folder.id, folder);
  }

  update(id: string, changes: Partial<FolderRecord>): void {
    const existing = this.folders.get(id);
    if (existing) this.folders.set(id, { ...existing, ...changes });
  }

  remove(id: string): void {
    this.folders.delete(id);
  }

  removeMany(ids: string[]): void {
    for (const id of ids) this.folders.delete(id);
  }

  hydrate(folders: FolderRecord[]): void {
    this.folders.clear();
    for (const folder of folders) this.folders.set(folder.id, folder);
  }

  clear(): void {
    this.folders.clear();
  }
}
