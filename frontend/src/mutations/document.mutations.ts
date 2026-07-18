import type { WorkspaceRootStore } from "@/stores/workspace-root.store";
import { getActiveRootStore } from "@/stores/root.store";
import type { SyncEngine } from "@/sync/sync-engine";
import type { DocumentRecord } from "@/types/projects/document-record";
import type { PendingTransaction } from "@/types/sync/transaction";
import { api } from "@/lib/api-client";
import { isConnectivityError, isNotFoundError } from "@/lib/is-connectivity-error";
import { devError } from "@/sync/dev-log";
import { fractionalAfter } from "@/features/workspace/contents/hierarchy/utils/fractional-index";
import { toJS } from "mobx";
import { toast } from "sonner";

export class DocumentMutations {
  private rootStore: WorkspaceRootStore;
  private syncEngine: SyncEngine;

  constructor(rootStore: WorkspaceRootStore, syncEngine: SyncEngine) {
    this.rootStore = rootStore;
    this.syncEngine = syncEngine;
  }

  async create(data: { spaceId: string; name: string; parentDocumentId?: string | null; icon?: string; color?: string }): Promise<DocumentRecord> {
    const id = crypto.randomUUID();
    const siblings = data.parentDocumentId
      ? this.rootStore.documentStore.getChildren(data.parentDocumentId)
      : this.rootStore.documentStore.getRootsBySpace(data.spaceId);
    const maxSiblingKey = siblings.reduce<string | null>(
      (max, d) => (d.orderKey && (!max || d.orderKey > max) ? d.orderKey : max),
      null,
    );
    const orderKey = fractionalAfter(maxSiblingKey);
    const icon = data.icon ?? "FileText";
    const color = data.color ?? "#ffffff";

    const record: DocumentRecord = {
      id,
      spaceId: data.spaceId,
      parentDocumentId: data.parentDocumentId ?? null,
      name: data.name,
      orderKey,
      icon,
      color,
      createdAt: new Date().toISOString(),
    };

    this.rootStore.documentStore.upsert(record);

    try {
      await this.rootStore.documentDB!.put(record);
    } catch (err) {
      this.rootStore.documentStore.remove(record.id);
      devError("[DocumentMutations] documentDB.put failed:", err);
      toast.error("Failed to save document locally. Please try again.");
      throw new Error(
        `Failed to persist document locally: ${err instanceof Error ? err.message : String(err)}`,
      );
    }

    const payload = {
      id,
      spaceId: data.spaceId,
      parentDocumentId: record.parentDocumentId,
      name: data.name,
      icon,
      color,
    };

    const tx = await this.syncEngine.transactionQueue.enqueue(
      "C",
      "Document",
      record.id,
      payload,
      null,
    );

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn("App is offline. Skipping API request. Will sync later.");
      return record;
    }

    try {
      await api.post("/documents/sync", payload, {
        headers: {
          "X-Workspace-Id": this.rootStore.workspaceId,
          "X-Client-Trace-Id": tx.id,
        },
      });
    } catch (err) {
      if (isConnectivityError(err)) {
        console.warn("You are offline. Document will sync when connection is restored.");
        return record;
      }

      this.rootStore.documentStore.remove(record.id);
      await this.rootStore.documentDB!.delete(record.id);
      await this.syncEngine.transactionQueue.dequeue(tx.id);
      throw err;
    }

    return record;
  }

  async updateLocal(
    documentId: string,
    changes: Partial<DocumentRecord> & { clearParent?: boolean },
  ): Promise<{ previous: DocumentRecord; tx: PendingTransaction }> {
    const stored = this.rootStore.documentStore.getById(documentId);
    if (!stored) throw new Error(`Document ${documentId} not found`);
    const previous = toJS(stored);
    const clearingParent = changes.clearParent === true;

    const merged = { ...previous, ...changes };
    if (clearingParent) merged.parentDocumentId = null;

    this.rootStore.documentStore.upsert(merged);

    try {
      await this.rootStore.documentDB!.put(merged);
    } catch {
      this.rootStore.documentStore.upsert(previous);
      toast.error("Failed to save document locally. Please try again.");
      throw new Error("Failed to persist update locally");
    }

    const payload: Record<string, unknown> = {};
    if ("name" in changes) payload.name = changes.name;
    if ("orderKey" in changes) payload.orderKey = changes.orderKey;
    if ("icon" in changes) payload.icon = changes.icon;
    if ("color" in changes) payload.color = changes.color;
    if (clearingParent) payload.clearParent = true;
    else if ("parentDocumentId" in changes) payload.parentDocumentId = changes.parentDocumentId;

    const tx = await this.syncEngine.transactionQueue.enqueue(
      "U",
      "Document",
      documentId,
      payload,
      previous as unknown as Record<string, unknown>,
    );

    return { previous, tx };
  }

  async update(
    documentId: string,
    changes: Partial<DocumentRecord> & { clearParent?: boolean },
  ): Promise<void> {
    const { previous, tx } = await this.updateLocal(documentId, changes);

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn("App is offline. Skipping API request. Will sync later.");
      return;
    }

    try {
      await api.put(`/documents/sync/${documentId}`, tx.data, {
        headers: {
          "X-Workspace-Id": this.rootStore.workspaceId,
          "X-Client-Trace-Id": tx.id,
        },
      });
    } catch (err) {
      if (isConnectivityError(err)) {
        console.warn("You are offline. Update will sync when connection is restored.");
        return;
      }

      this.rootStore.documentStore.upsert(previous);
      await this.rootStore.documentDB!.put(previous);
      await this.syncEngine.transactionQueue.dequeue(tx.id);
      throw err;
    }
  }

  // Deviates from Folder (which reparents orphaned Tasks): cascades the whole subtree + their
  // DocumentBlocks. Deleting a wiki page should remove its sub-pages, not orphan them at root.
  async delete(documentId: string): Promise<void> {
    const stored = this.rootStore.documentStore.getById(documentId);
    if (!stored) throw new Error(`Document ${documentId} not found`);
    const previous = toJS(stored);

    const descendantIds = this.rootStore.documentStore.getDescendantIds(documentId);

    await this.syncEngine.transactionQueue.cancelByEntityIds(descendantIds);

    this.rootStore.documentStore.removeMany(descendantIds);
    for (const id of descendantIds) this.rootStore.documentBlockStore.removeByDocument(id);

    try {
      await this.rootStore.documentDB!.deleteMany(descendantIds);
      await this.rootStore.documentBlockDB!.deleteByDocumentIds(descendantIds);
    } catch {
      this.rootStore.documentStore.upsert(previous);
      toast.error("Failed to delete document locally. Please try again.");
      throw new Error("Failed to persist delete locally");
    }

    const tx = await this.syncEngine.transactionQueue.enqueue(
      "D",
      "Document",
      documentId,
      { id: documentId },
      previous as unknown as Record<string, unknown>,
    );

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn("App is offline. Skipping API request. Will sync later.");
      return;
    }

    try {
      await api.delete(`/documents/sync/${documentId}`, {
        headers: {
          "X-Workspace-Id": this.rootStore.workspaceId,
          "X-Client-Trace-Id": tx.id,
        },
      });
    } catch (err) {
      if (isConnectivityError(err)) {
        console.warn("You are offline. Deletion will sync when connection is restored.");
        return;
      }

      if (isNotFoundError(err)) {
        await this.syncEngine.transactionQueue.dequeue(tx.id);
        return;
      }

      // Only the top-level document is restored here — the descendants removed above aren't
      // snapshotted for rollback, matching Space's cascade delete (which likewise only restores
      // the Space itself on failure, not the Folders/Tasks it already cascaded locally).
      this.rootStore.documentStore.upsert(previous);
      await this.rootStore.documentDB!.put(previous);
      await this.syncEngine.transactionQueue.dequeue(tx.id);
      throw err;
    }
  }
}
