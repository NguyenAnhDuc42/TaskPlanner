import type { IDBPDatabase } from "idb";
import type { TaskPlanDB } from "../schema";
import type { MemberRecord } from "@/types/workspace/member-record";

export class MemberDB {
  private db: IDBPDatabase<TaskPlanDB>

  constructor(db: IDBPDatabase<TaskPlanDB>) {
    this.db = db
  }

  async get(id: string): Promise<MemberRecord | undefined> {
    return this.db.get('members', id)
  }

  async getAll(): Promise<MemberRecord[]> {
    return this.db.getAll('members')
  }

  async put(member: MemberRecord): Promise<void> {
    await this.db.put('members', member)
  }

  async putMany(members: MemberRecord[]): Promise<void> {
    const tx = this.db.transaction('members', 'readwrite')
    await Promise.all([
      ...members.map((m) => tx.store.put(m)),
      tx.done,
    ])
  }

  async delete(id: string): Promise<void> {
    await this.db.delete('members', id)
  }

  async clear(): Promise<void> {
    await this.db.clear('members')
  }
}
