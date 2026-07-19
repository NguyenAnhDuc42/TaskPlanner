import { makeAutoObservable, observable, computed } from "mobx";
import type { DocumentBlockRecord } from "@/types/document";

const EMPTY_BLOCKS: DocumentBlockRecord[] = [];

export class DocumentBlockStore {
  blocks = observable.map<string, DocumentBlockRecord>({}, { deep: false });

  constructor() {
    // keepAlive — see TaskStore constructor for why.
    makeAutoObservable<DocumentBlockStore, "byDocumentIndex">(this, {
      byDocumentIndex: computed({ keepAlive: true }),
    });
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
      list.sort((a, b) => (a.orderKey !== b.orderKey ? (a.orderKey < b.orderKey ? -1 : 1) : a.id < b.id ? -1 : a.id > b.id ? 1 : 0));
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

  upsertMany(blocks: DocumentBlockRecord[]): void {
    for (const b of blocks) {
      this.blocks.set(b.id, b);
    }
  }

  remove(id: string): void {
    this.blocks.delete(id);
  }

  removeByDocument(documentId: string): void {
    for (const block of this.blocks.values()) {
      if (block.documentId === documentId) this.blocks.delete(block.id);
    }
  }

  private static readonly MAX_RELEASED_DOCUMENTS = 8;
  private releasedOrder: string[] = [];

  retainDocument(documentId: string): void {
    this.releasedOrder = this.releasedOrder.filter((id) => id !== documentId);
  }

  releaseDocument(documentId: string): void {
    const next = this.releasedOrder.filter((id) => id !== documentId);
    next.push(documentId);
    while (next.length > DocumentBlockStore.MAX_RELEASED_DOCUMENTS) {
      this.removeByDocument(next.shift()!);
    }
    this.releasedOrder = next;
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
