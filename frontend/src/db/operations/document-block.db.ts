import type { IDBPDatabase } from "idb";
import type { TaskPlanDB } from "../schema";
import type { DocumentBlockRecord } from "@/types/document";

export class DocumentBlockDB {
  private db: IDBPDatabase<TaskPlanDB>

  constructor(db: IDBPDatabase<TaskPlanDB>) {
    this.db = db
  }

  async get(id: string): Promise<DocumentBlockRecord | undefined> {
    return this.db.get('document_blocks', id)
  }

  async getAll(): Promise<DocumentBlockRecord[]> {
    return this.db.getAll('document_blocks')
  }

  async getAllByDocument(documentId: string): Promise<DocumentBlockRecord[]> {
    return this.db.getAllFromIndex('document_blocks', 'by-document', documentId)
  }

  async put(block: DocumentBlockRecord): Promise<void> {
    await this.db.put('document_blocks', block)
  }

  async putMany(blocks: DocumentBlockRecord[]): Promise<void> {
    const tx = this.db.transaction('document_blocks', 'readwrite')
    await Promise.all([
      ...blocks.map((b) => tx.store.put(b)),
      tx.done,
    ])
  }

  async applyBatch(puts: DocumentBlockRecord[], deleteIds: string[]): Promise<void> {
    if (puts.length === 0 && deleteIds.length === 0) return
    const tx = this.db.transaction('document_blocks', 'readwrite')
    await Promise.all([
      ...puts.map((b) => tx.store.put(b)),
      ...deleteIds.map((id) => tx.store.delete(id)),
      tx.done,
    ])
  }

  async delete(id: string): Promise<void> {
    await this.db.delete('document_blocks', id)
  }

  // Cascade cleanup when a Document (or a whole subtree of them) is deleted — purges every block
  // belonging to any of the given document ids that happens to be cached locally.
  async deleteByDocumentIds(documentIds: string[]): Promise<void> {
    if (documentIds.length === 0) return
    const tx = this.db.transaction('document_blocks', 'readwrite')
    const index = tx.store.index('by-document')
    await Promise.all(
      documentIds.map(async (documentId) => {
        let cursor = await index.openCursor(IDBKeyRange.only(documentId))
        while (cursor) {
          await cursor.delete()
          cursor = await cursor.continue()
        }
      }),
    )
    await tx.done
  }

  async clear(): Promise<void> {
    await this.db.clear('document_blocks')
  }
}
