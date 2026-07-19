import type { DocumentRecord } from "@/types/projects";
import { makeAutoObservable, observable, computed } from "mobx";

const EMPTY_DOCUMENTS: DocumentRecord[] = [];
const ROOT_KEY = "__root__";

const byOrderKey = (a: DocumentRecord, b: DocumentRecord) => {
  const ka = a.orderKey ?? "";
  const kb = b.orderKey ?? "";
  if (ka !== kb) return ka < kb ? -1 : 1;
  return a.id < b.id ? -1 : a.id > b.id ? 1 : 0;
};

export class DocumentStore {
  documents = observable.map<string, DocumentRecord>({}, { deep: false });

  constructor() {
    // keepAlive — see TaskStore constructor for why. Especially load-bearing here: getDescendantIds
    // BFS-calls getChildren once per node, and document.mutations.ts/delta-handler.ts/
    // handle-document-move.ts all read these indexes from plain (non-observer) code.
    makeAutoObservable<DocumentStore, "rootsBySpaceIndex" | "byParentIndex">(this, {
      rootsBySpaceIndex: computed({ keepAlive: true }),
      byParentIndex: computed({ keepAlive: true }),
    });
  }

  get all(): DocumentRecord[] {
    return Array.from(this.documents.values());
  }

  // Unlike Folder (fixed one level under a Space), Documents nest to unbounded depth — so two
  // separate indexes are needed: root pages grouped by space, and children grouped by parent.
  private get rootsBySpaceIndex(): Map<string, DocumentRecord[]> {
    const index = new Map<string, DocumentRecord[]>();
    for (const doc of this.documents.values()) {
      if (doc.parentDocumentId) continue;
      const list = index.get(doc.spaceId);
      if (list) list.push(doc);
      else index.set(doc.spaceId, [doc]);
    }
    for (const list of index.values()) list.sort(byOrderKey);
    return index;
  }

  private get byParentIndex(): Map<string, DocumentRecord[]> {
    const index = new Map<string, DocumentRecord[]>();
    for (const doc of this.documents.values()) {
      const key = doc.parentDocumentId ?? ROOT_KEY;
      if (key === ROOT_KEY) continue;
      const list = index.get(key);
      if (list) list.push(doc);
      else index.set(key, [doc]);
    }
    for (const list of index.values()) list.sort(byOrderKey);
    return index;
  }

  getById(id: string): DocumentRecord | undefined {
    return this.documents.get(id);
  }

  getRootsBySpace(spaceId: string): DocumentRecord[] {
    return this.rootsBySpaceIndex.get(spaceId) ?? EMPTY_DOCUMENTS;
  }

  getChildren(documentId: string): DocumentRecord[] {
    return this.byParentIndex.get(documentId) ?? EMPTY_DOCUMENTS;
  }

  // BFS over the parent index — includes documentId itself. Reused by cascade delete (this
  // document + everything under it) and by the DnD cycle-check (is the drop target one of the
  // dragged document's own descendants).
  getDescendantIds(documentId: string): string[] {
    const result: string[] = [documentId];
    const queue = [documentId];
    while (queue.length > 0) {
      const current = queue.shift()!;
      for (const child of this.getChildren(current)) {
        result.push(child.id);
        queue.push(child.id);
      }
    }
    return result;
  }

  upsert(document: DocumentRecord): void {
    this.documents.set(document.id, document);
  }

  upsertMany(documents: DocumentRecord[]): void {
    for (const document of documents) this.upsert(document);
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
