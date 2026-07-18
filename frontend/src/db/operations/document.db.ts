import type { IDBPDatabase } from "idb";
import type { TaskPlanDB } from "../schema";
import type { DocumentRecord } from "@/types/projects/document-record";

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

  async getAllBySpace(spaceId: string): Promise<DocumentRecord[]> {
    return this.db.getAllFromIndex('documents', 'by-space', spaceId)
  }

  async getAllByParent(parentDocumentId: string): Promise<DocumentRecord[]> {
    return this.db.getAllFromIndex('documents', 'by-parent', parentDocumentId)
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

  // Needed for cascade delete — a Document delete removes its whole subtree in one shot,
  // unlike Folder which only ever deletes a single row.
  async deleteMany(ids: string[]): Promise<void> {
    if (ids.length === 0) return
    const tx = this.db.transaction('documents', 'readwrite')
    await Promise.all([
      ...ids.map((id) => tx.store.delete(id)),
      tx.done,
    ])
  }

  async clear(): Promise<void> {
    await this.db.clear('documents')
  }
}
