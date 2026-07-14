import { makeAutoObservable, observable } from "mobx";
import type { DocumentBlockRecord } from "@/types/document";

export class DocumentBlockStore {
  // deep: false — records are always replaced wholesale (set with a new object), never mutated
  // field-by-field, so wrapping every record in a deep observable (one atom per property) is
  // pure memory overhead. Shallow maps still react to set/delete/get like before.
  blocks = observable.map<string, DocumentBlockRecord>({}, { deep: false });

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

  // Single MobX action = single transaction: observers and reactions tracking the map fire once
  // for the whole batch instead of once per block. Loading a document's blocks via a plain
  // per-item upsert loop re-runs every tracking reaction N times, each doing a full-map scan.
  upsertMany(blocks: DocumentBlockRecord[]): void {
    for (const b of blocks) {
      this.blocks.set(b.id, b);
    }
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
