import type { SpaceRecord } from "@/types/projects";
import { makeAutoObservable, observable } from "mobx";

export class SpaceStore {
  // deep: false — records replaced wholesale, never mutated in place. See DocumentBlockStore.
  spaces = observable.map<string, SpaceRecord>({}, { deep: false });

  constructor() {
    makeAutoObservable(this);
  }

  get all(): SpaceRecord[] {
    return Array.from(this.spaces.values());
  }

  getById(id: string): SpaceRecord | undefined {
    return this.spaces.get(id);
  }

  upsert(space: SpaceRecord): void {
    this.spaces.set(space.id, space);
  }

  upsertMany(spaces: SpaceRecord[]): void {
    for (const space of spaces) {
      this.upsert(space);
    }
  }

  update(id: string, changes: Partial<SpaceRecord>): void {
    const existing = this.spaces.get(id);
    if (existing) this.spaces.set(id, { ...existing, ...changes });
  }

  remove(id: string): void {
    this.spaces.delete(id);
  }

  removeMany(ids: string[]): void {
    for (const id of ids) this.spaces.delete(id);
  }

  hydrate(spaces: SpaceRecord[]): void {
    this.spaces.clear();
    for (const space of spaces) this.spaces.set(space.id, space);
  }

  clear(): void {
    this.spaces.clear();
  }
}
