import { makeAutoObservable, observable } from "mobx";
import type { DocumentBlockRecord } from "@/types/document";

const EMPTY_BLOCKS: DocumentBlockRecord[] = [];

export class DocumentBlockStore {
  blocks = observable.map<string, DocumentBlockRecord>({}, { deep: false });

  constructor() {
    makeAutoObservable(this);
  }

  get all(): DocumentBlockRecord[] {
    return Array.from(this.blocks.values());
  }

  private get byDocumentIndex(): Map<string, DocumentBlockRecord[]> {
    const index = new Map<string, DocumentBlockRecord[]>();
    for (const block of this.blocks.values()) {
      const list = index.get(block.documentId);
      if (list) list.push(block);
      else index.set(block.documentId, [block]);
    }
    for (const list of index.values()) {
      list.sort((a, b) => a.orderKey.localeCompare(b.orderKey));
    }
    return index;
  }

  getById(id: string): DocumentBlockRecord | undefined {
    return this.blocks.get(id);
  }

  getByDocument(documentId: string): DocumentBlockRecord[] {
    return this.byDocumentIndex.get(documentId) ?? EMPTY_BLOCKS;
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
