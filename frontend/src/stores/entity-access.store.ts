import { makeAutoObservable, observable } from "mobx";
import type { EntityAccessRecord } from "@/types/workspace/entity-access-record";

export class EntityAccessStore {
  records = observable.map<string, EntityAccessRecord>();

  constructor() {
    makeAutoObservable(this);
  }

  get all(): EntityAccessRecord[] {
    return Array.from(this.records.values());
  }

  getById(id: string): EntityAccessRecord | undefined {
    return this.records.get(id);
  }

  getBySpace(spaceId: string): EntityAccessRecord[] {
    return this.all.filter((r) => r.spaceId === spaceId);
  }

  upsert(record: EntityAccessRecord): void {
    this.records.set(record.id, record);
  }

  remove(id: string): void {
    this.records.delete(id);
  }

  hydrate(records: EntityAccessRecord[]): void {
    this.records.clear();
    for (const r of records) {
      this.records.set(r.id, r);
    }
  }

  clear(): void {
    this.records.clear();
  }
}
