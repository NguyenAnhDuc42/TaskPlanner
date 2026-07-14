import type { WorkspaceRootStore } from "@/stores/workspace-root.store";
import { getActiveRootStore } from "@/stores/root.store";
import type { SyncEngine } from "@/sync/sync-engine";
import type { FolderRecord } from "@/types/projects/folder-record";
import type { PendingTransaction } from "@/types/sync/transaction";
import { api } from "@/lib/api-client";
import { isConnectivityError, isNotFoundError } from "@/lib/is-connectivity-error";
import { devError } from "@/sync/dev-log";
import { fractionalAfter } from "@/features/workspace/contents/hierarchy/utils/fractional-index";
import { toJS } from "mobx";
import { toast } from "sonner";

export class FolderMutations {
  private rootStore: WorkspaceRootStore;
  private syncEngine: SyncEngine;

  constructor(rootStore: WorkspaceRootStore, syncEngine: SyncEngine) {
    this.rootStore = rootStore;
    this.syncEngine = syncEngine;
  }

  async create(
    data: Omit<FolderRecord, "id" | "createdAt"> & { spaceId: string },
  ): Promise<FolderRecord> {
    const id = crypto.randomUUID();
    const siblings = this.rootStore.folderStore.getBySpace(data.spaceId);
    const maxSiblingKey = siblings.reduce<string | null>(
      (max, f) => (f.orderKey && (!max || f.orderKey > max) ? f.orderKey : max),
      null,
    );
    const orderKey = data.orderKey ?? fractionalAfter(maxSiblingKey);

    const record: FolderRecord = {
      ...data,
      id,
      orderKey,
      createdAt: new Date().toISOString(),
    };

    this.rootStore.folderStore.upsert(record);

    try {
      await this.rootStore.folderDB!.put(record);
    } catch (err) {
      this.rootStore.folderStore.remove(record.id);
      devError("[FolderMutations] folderDB.put failed:", err);
      toast.error("Failed to save folder locally. Please try again.");
      throw new Error(
        `Failed to persist folder locally: ${err instanceof Error ? err.message : String(err)}`,
      );
    }

    const payload = {
      id,
      spaceId: data.spaceId,
      name: data.name,
      color: data.color ?? null,
      icon: data.icon ?? null,
      startDate: data.startDate ?? null,
      dueDate: data.dueDate ?? null,
    };

    const tx = await this.syncEngine.transactionQueue.enqueue(
      "C",
      "Folder",
      record.id,
      payload,
      null,
    );

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn("App is offline. Skipping API request. Will sync later.");
      return record;
    }

    try {
      await api.post("/folders/sync", payload, {
        headers: {
          "X-Workspace-Id": this.rootStore.workspaceId,
          "X-Client-Trace-Id": tx.id,
        },
      });
    } catch (err) {
      if (isConnectivityError(err)) {
        console.warn(
          "You are offline. Folder will sync when connection is restored.",
        );
        return record;
      }

      this.rootStore.folderStore.remove(record.id);
      await this.rootStore.folderDB!.delete(record.id);
      await this.syncEngine.transactionQueue.dequeue(tx.id);
      throw err;
    }

    return record;
  }

  async updateLocal(
    folderId: string,
    changes: Partial<FolderRecord>,
  ): Promise<{ previous: FolderRecord; tx: PendingTransaction }> {
    const stored = this.rootStore.folderStore.getById(folderId);
    if (!stored) throw new Error(`Folder ${folderId} not found`);
    const previous = toJS(stored);
    const clearingStartDate = changes.startDate === null;
    const clearingDueDate = changes.dueDate === null;

    const merged = { ...previous, ...changes };

    this.rootStore.folderStore.upsert(merged);

    try {
      await this.rootStore.folderDB!.put(merged);
    } catch {
      this.rootStore.folderStore.upsert(previous);
      toast.error("Failed to save folder locally. Please try again.");
      throw new Error("Failed to persist update locally");
    }

    const payload: Record<string, unknown> = {};
    if ("name" in changes) payload.name = changes.name;
    if ("color" in changes) payload.color = changes.color;
    if ("icon" in changes) payload.icon = changes.icon;
    if ("orderKey" in changes) payload.orderKey = changes.orderKey;
    if (clearingStartDate) payload.clearStartDate = true;
    else if ("startDate" in changes)
      payload.startDate = changes.startDate;
    if (clearingDueDate) payload.clearDueDate = true;
    else if ("dueDate" in changes) payload.dueDate = changes.dueDate;
    if ("spaceId" in changes) payload.spaceId = changes.spaceId;

    const tx = await this.syncEngine.transactionQueue.enqueue(
      "U",
      "Folder",
      folderId,
      payload,
      previous as unknown as Record<string, unknown>,
    );

    return { previous, tx };
  }

  async update(
    folderId: string,
    changes: Partial<FolderRecord>,
  ): Promise<void> {
    const { previous, tx } = await this.updateLocal(folderId, changes);

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn("App is offline. Skipping API request. Will sync later.");
      return;
    }

    try {
      await api.put(`/folders/sync/${folderId}`, tx.data, {
        headers: {
          "X-Workspace-Id": this.rootStore.workspaceId,
          "X-Client-Trace-Id": tx.id,
        },
      });
    } catch (err) {
      if (isConnectivityError(err)) {
        console.warn(
          "You are offline. Update will sync when connection is restored.",
        );
        return;
      }

      this.rootStore.folderStore.upsert(previous);
      await this.rootStore.folderDB!.put(previous);
      await this.syncEngine.transactionQueue.dequeue(tx.id);
      throw err;
    }
  }

  async delete(folderId: string): Promise<void> {
    const stored = this.rootStore.folderStore.getById(folderId);
    if (!stored) throw new Error(`Folder ${folderId} not found`);
    const previous = toJS(stored);

    for (const task of [...this.rootStore.taskStore.getByFolder(folderId)]) {
      const reparented = { ...toJS(task), folderId: null };
      this.rootStore.taskStore.upsert(reparented);
      await this.rootStore.taskDB!.put(reparented);
    }

    this.rootStore.folderStore.remove(folderId);

    try {
      await this.rootStore.folderDB!.delete(folderId);
    } catch {
      this.rootStore.folderStore.upsert(previous);
      toast.error("Failed to delete folder locally. Please try again.");
      throw new Error("Failed to persist delete locally");
    }

    const tx = await this.syncEngine.transactionQueue.enqueue(
      "D",
      "Folder",
      folderId,
      { id: folderId },
      previous as unknown as Record<string, unknown>,
    );

    if (!(getActiveRootStore()?.isOnline ?? true)) {
      console.warn("App is offline. Skipping API request. Will sync later.");
      return;
    }

    try {
      await api.delete(`/folders/sync/${folderId}`, {
        headers: {
          "X-Workspace-Id": this.rootStore.workspaceId,
          "X-Client-Trace-Id": tx.id,
        },
      });
    } catch (err) {
      if (isConnectivityError(err)) {
        console.warn(
          "You are offline. Deletion will sync when connection is restored.",
        );
        return;
      }

      if (isNotFoundError(err)) {
        await this.syncEngine.transactionQueue.dequeue(tx.id);
        return;
      }

      this.rootStore.folderStore.upsert(previous);
      await this.rootStore.folderDB!.put(previous);
      await this.syncEngine.transactionQueue.dequeue(tx.id);
      throw err;
    }
  }
}
