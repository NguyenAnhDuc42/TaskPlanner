import type { IDBPDatabase } from "idb";
import type { TaskPlanDB } from "../schema";
import type { TaskRecord } from "@/types/projects/task-record";

export class TaskDB {

  private db: IDBPDatabase<TaskPlanDB>

  constructor(db: IDBPDatabase<TaskPlanDB>) {
    this.db = db
  }
  
  async get(id:string) : Promise<TaskRecord | undefined>{
    return this.db.get('tasks',id)
  }
  async getAll() : Promise<TaskRecord[]>{
    return this.db.getAll('tasks')
  }

  async getAllBySpace(spaceId:string) : Promise<TaskRecord[]>{
    return this.db.getAllFromIndex('tasks','by-space',spaceId)
  }

  async getAllByFolder(folderId:string) : Promise<TaskRecord[]>{
    return this.db.getAllFromIndex('tasks','by-folder',folderId)
  }

  async getSubTasks(parentTaskId:string) : Promise<TaskRecord[]>{
    return this.db.getAllFromIndex('tasks','by-parent-task',parentTaskId)
  }

  async getAllByStatus(statusId:string) : Promise<TaskRecord[]>{
    return this.db.getAllFromIndex('tasks','by-status',statusId)
  }

  async put(task:TaskRecord) : Promise<void> {
    await this.db.put('tasks',task)
  }

  async putMany(tasks:TaskRecord[]) : Promise<void> {
    const transactions = this.db.transaction('tasks','readwrite')
    await Promise.all([
      ...tasks.map((task) => transactions.store.put(task)),
      transactions.done,
    ])
  }

  async delete(id:string) : Promise<void> {
    await this.db.delete('tasks',id)
  }

  async clear(): Promise<void> {
    await this.db.clear('tasks')
  }
}
