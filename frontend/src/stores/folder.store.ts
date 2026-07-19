import type { FolderRecord } from "@/types/projects";
import { makeAutoObservable, observable, computed } from "mobx";

const EMPTY_FOLDERS: FolderRecord[] = [];

const byOrderKey = (a: FolderRecord, b: FolderRecord) => {
  const ka = a.orderKey ?? "";
  const kb = b.orderKey ?? "";
  if (ka !== kb) return ka < kb ? -1 : 1;
  return a.id < b.id ? -1 : a.id > b.id ? 1 : 0;
};

export class FolderStore {
  folders = observable.map<string, FolderRecord>({}, { deep: false });

  constructor() {
    // keepAlive — see TaskStore constructor for why.
    makeAutoObservable<FolderStore, "bySpaceIndex">(this, {
      bySpaceIndex: computed({ keepAlive: true }),
    });
  }

  get all(): FolderRecord[] {
    return Array.from(this.folders.values());
  }

  private get bySpaceIndex(): Map<string, FolderRecord[]> {
    const index = new Map<string, FolderRecord[]>();
    for (const folder of this.folders.values()) {
      if (!folder.spaceId) continue;
      const list = index.get(folder.spaceId);
      if (list) list.push(folder);
      else index.set(folder.spaceId, [folder]);
    }
    for (const list of index.values()) list.sort(byOrderKey);
    return index;
  }

  getById(id: string): FolderRecord | undefined {
    return this.folders.get(id);
  }

  getBySpace(spaceId: string): FolderRecord[] {
    return this.bySpaceIndex.get(spaceId) ?? EMPTY_FOLDERS;
  }

  upsert(folder: FolderRecord): void {
    this.folders.set(folder.id, folder);
  }

  upsertMany(folders: FolderRecord[]): void {
    for (const folder of folders) this.upsert(folder);
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
