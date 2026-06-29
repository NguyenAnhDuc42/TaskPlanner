import { makeAutoObservable, observable } from "mobx";
import type { DocumentBlockRecord } from "@/types/document";

export class DocumentBlockStore {
  blocks = observable.map<string, DocumentBlockRecord>();

  constructor() {
    makeAutoObservable(this);
  }

  get all(): DocumentBlockRecord[] {
    return Array.from(this.blocks.values());
  }

  getById(id: string): DocumentBlockRecord | undefined {
    return this.blocks.get(id);
  }

  getByDocument(documentId: string): DocumentBlockRecord[] {
    return this.all.filter((b) => b.documentId === documentId).sort((a, b) => a.orderKey.localeCompare(b.orderKey));
  }

  upsert(block: DocumentBlockRecord): void {
    this.blocks.set(block.id, block);
  }

  remove(id: string): void {
    this.blocks.delete(id);
  }

  hydrate(blocks: DocumentBlockRecord[]): void {
    this.blocks.clear();
    for (const b of blocks) {
      this.blocks.set(b.id, b);
    }
  }

  clear(): void {
    this.blocks.clear();
  }
}
