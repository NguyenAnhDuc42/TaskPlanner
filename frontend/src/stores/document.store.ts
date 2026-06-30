import type { DocumentRecord } from "@/types/document";
import { makeAutoObservable, observable } from "mobx";

export class DocumentStore {
  documents = observable.map<string, DocumentRecord>();

  constructor() {
    makeAutoObservable(this);
  }

  get all(): DocumentRecord[] {
    return Array.from(this.documents.values());
  }

  getById(id: string): DocumentRecord | undefined {
    return this.documents.get(id);
  }

  upsert(document: DocumentRecord): void {
    this.documents.set(document.id, document);
  }

  upsertMany(documents: DocumentRecord[]): void {
    for (const document of documents) this.documents.set(document.id, document);
  }

  update(id: string, changes: Partial<DocumentRecord>): void {
    const existing = this.documents.get(id);
    if (existing) this.documents.set(id, { ...existing, ...changes });
  }

  remove(id: string): void {
    this.documents.delete(id);
  }

  removeMany(ids: string[]): void {
    for (const id of ids) this.documents.delete(id);
  }

  hydrate(documents: DocumentRecord[]): void {
    this.documents.clear();
    for (const document of documents) this.documents.set(document.id, document);
  }

  clear(): void {
    this.documents.clear();
  }
}
