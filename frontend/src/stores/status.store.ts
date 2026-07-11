import type { Status } from "@/types/status";
import { makeAutoObservable, observable } from "mobx";

export class StatusStore {
  statuses = observable.map<string, Status>();

  constructor() {
    makeAutoObservable(this);
  }

  get all(): Status[] {
    return Array.from(this.statuses.values());
  }

  getById(id: string): Status | undefined {
    return this.statuses.get(id);
  }

  getBySpace(spaceId: string): Status[] {
    return this.all.filter((s) => s.spaceId === spaceId);
  }

  getVisibleForSpace(spaceId: string): Status[] {
    return this.all.filter((s) => !s.spaceId || s.spaceId === spaceId);
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
