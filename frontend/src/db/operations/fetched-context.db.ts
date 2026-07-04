import type { IDBPDatabase } from "idb";
import type { TaskPlanDB } from "../schema";

// Backs the `lazy` load tier's "have I already fetched this" guard — see FRONTEND_SYNC_CONTEXT.md
// §1b. A context is a plain string key like `comments:{taskId}` or `document_blocks:{documentId}`.
export class FetchedContextDB {
  private db: IDBPDatabase<TaskPlanDB>

  constructor(db: IDBPDatabase<TaskPlanDB>) {
    this.db = db
  }

  async hasFetched(context: string): Promise<boolean> {
    const record = await this.db.get('__fetched_contexts', context)
    return record !== undefined
  }

  async markFetched(context: string): Promise<void> {
    await this.db.put('__fetched_contexts', { context, fetchedAt: new Date().toISOString() }, context)
  }

  async clear(): Promise<void> {
    await this.db.clear('__fetched_contexts')
  }
}
