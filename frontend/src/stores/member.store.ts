import type { MemberRecord } from "@/types/workspace";
import { makeAutoObservable, observable } from "mobx";

export class MemberStore {
  members = observable.map<string, MemberRecord>();

  constructor() {
    makeAutoObservable(this);
  }

  get all(): MemberRecord[] {
    return Array.from(this.members.values());
  }

  getById(id: string): MemberRecord | undefined {
    return this.members.get(id);
  }

  getByUserId(userId: string): MemberRecord | undefined {
    return this.all.find((m) => m.userId === userId);
  }

  upsert(member: MemberRecord): void {
    this.members.set(member.id, member);
  }

  upsertMany(members: MemberRecord[]): void {
    for (const member of members) this.members.set(member.id, member);
  }

  update(id: string, changes: Partial<MemberRecord>): void {
    const existing = this.members.get(id);
    if (existing) this.members.set(id, { ...existing, ...changes });
  }

  remove(id: string): void {
    this.members.delete(id);
  }

  removeMany(ids: string[]): void {
    for (const id of ids) this.members.delete(id);
  }

  hydrate(members: MemberRecord[]): void {
    this.members.clear();
    for (const member of members) this.members.set(member.id, member);
  }

  clear(): void {
    this.members.clear();
  }
}
