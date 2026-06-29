import type { IDBPDatabase } from "idb";
import type { TaskPlanDB } from "../schema";
import type { CommentRecord } from "@/types/projects/comment-record";

export class CommentDB {
  private db: IDBPDatabase<TaskPlanDB>

  constructor(db: IDBPDatabase<TaskPlanDB>) {
    this.db = db
  }

  async get(id: string): Promise<CommentRecord | undefined> {
    return this.db.get('comments', id)
  }

  async getAll(): Promise<CommentRecord[]> {
    return this.db.getAll('comments')
  }

  async getAllByTask(taskId: string): Promise<CommentRecord[]> {
    return this.db.getAllFromIndex('comments', 'by-task', taskId)
  }

  async put(comment: CommentRecord): Promise<void> {
    await this.db.put('comments', comment)
  }

  async putMany(comments: CommentRecord[]): Promise<void> {
    const tx = this.db.transaction('comments', 'readwrite')
    await Promise.all([
      ...comments.map((c) => tx.store.put(c)),
      tx.done,
    ])
  }

  async delete(id: string): Promise<void> {
    await this.db.delete('comments', id)
  }

  async clear(): Promise<void> {
    await this.db.clear('comments')
  }
}
