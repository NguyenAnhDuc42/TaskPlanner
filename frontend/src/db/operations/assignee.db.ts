import type { IDBPDatabase } from "idb";
import type { TaskPlanDB } from "../schema";
import type { AssigneeRecord } from "@/types/projects/assignee-record";

export class AssigneeDB {
  private db: IDBPDatabase<TaskPlanDB>

  constructor(db: IDBPDatabase<TaskPlanDB>) {
    this.db = db
  }

  async get(id: string): Promise<AssigneeRecord | undefined> {
    return this.db.get('assignees', id)
  }

  async getAll(): Promise<AssigneeRecord[]> {
    return this.db.getAll('assignees')
  }

  async getAllByTask(taskId: string): Promise<AssigneeRecord[]> {
    return this.db.getAllFromIndex('assignees', 'by-task', taskId)
  }

  async put(assignee: AssigneeRecord): Promise<void> {
    await this.db.put('assignees', assignee)
  }

  async putMany(assignees: AssigneeRecord[]): Promise<void> {
    const tx = this.db.transaction('assignees', 'readwrite')
    await Promise.all([
      ...assignees.map((a) => tx.store.put(a)),
      tx.done,
    ])
  }

  async delete(id: string): Promise<void> {
    await this.db.delete('assignees', id)
  }

  async clear(): Promise<void> {
    await this.db.clear('assignees')
  }
}
