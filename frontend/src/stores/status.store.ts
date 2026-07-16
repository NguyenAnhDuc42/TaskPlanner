import type { Status } from "@/types/status";
import { makeAutoObservable, observable } from "mobx";

const EMPTY_STATUSES: Status[] = [];

const byOrderKey = (a: Status, b: Status) => {
  const ka = a.orderKey ?? "";
  const kb = b.orderKey ?? "";
  if (ka !== kb) return ka < kb ? -1 : 1;
  return a.id < b.id ? -1 : a.id > b.id ? 1 : 0;
};

export class StatusStore {
  statuses = observable.map<string, Status>({}, { deep: false });

  constructor() {
    makeAutoObservable(this);
  }

  get all(): Status[] {
    return Array.from(this.statuses.values());
  }

  private get globalStatuses(): Status[] {
    return this.all.filter((s) => !s.spaceId).sort(byOrderKey);
  }

  private get bySpaceIndex(): Map<string, Status[]> {
    const index = new Map<string, Status[]>();
    for (const status of this.statuses.values()) {
      if (!status.spaceId) continue;
      const list = index.get(status.spaceId);
      if (list) list.push(status);
      else index.set(status.spaceId, [status]);
    }
    for (const list of index.values()) list.sort(byOrderKey);
    return index;
  }

  private get visibleBySpaceIndex(): Map<string, Status[]> {
    const index = new Map<string, Status[]>();
    for (const [spaceId, own] of this.bySpaceIndex) {
      index.set(spaceId, [...this.globalStatuses, ...own].sort(byOrderKey));
    }
    return index;
  }

  getById(id: string): Status | undefined {
    return this.statuses.get(id);
  }

  getBySpace(spaceId: string): Status[] {
    return this.bySpaceIndex.get(spaceId) ?? EMPTY_STATUSES;
  }

  getVisibleForSpace(spaceId: string): Status[] {
    return this.visibleBySpaceIndex.get(spaceId) ?? this.globalStatuses;
  }

  upsert(status: Status): void {
    this.statuses.set(status.id, status);
  }

  upsertMany(statuses: Status[]): void {
    for (const status of statuses) this.statuses.set(status.id, status);
  }

  update(id: string, changes: Partial<Status>): void {
    const existing = this.statuses.get(id);
    if (existing) this.statuses.set(id, { ...existing, ...changes });
  }

  remove(id: string): void {
    this.statuses.delete(id);
  }

  removeMany(ids: string[]): void {
    for (const id of ids) this.statuses.delete(id);
  }

  hydrate(statuses: Status[]): void {
    this.statuses.clear();
    for (const status of statuses) this.statuses.set(status.id, status);
  }

  clear(): void {
    this.statuses.clear();
  }
}
