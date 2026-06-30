import type { IDBPDatabase } from "idb";
import type { TaskPlanDB } from "../schema";
import type { DocumentRecord } from "@/types/document";

export class DocumentDB {
  private db: IDBPDatabase<TaskPlanDB>

  constructor(db: IDBPDatabase<TaskPlanDB>) {
    this.db = db
  }

  async get(id: string): Promise<DocumentRecord | undefined> {
    return this.db.get('documents', id)
  }

  async getAll(): Promise<DocumentRecord[]> {
    return this.db.getAll('documents')
  }

  async put(document: DocumentRecord): Promise<void> {
    await this.db.put('documents', document)
  }

  async putMany(documents: DocumentRecord[]): Promise<void> {
    const tx = this.db.transaction('documents', 'readwrite')
    await Promise.all([
      ...documents.map((d) => tx.store.put(d)),
      tx.done,
    ])
  }

  async delete(id: string): Promise<void> {
    await this.db.delete('documents', id)
  }

  async clear(): Promise<void> {
    await this.db.clear('documents')
  }
}
