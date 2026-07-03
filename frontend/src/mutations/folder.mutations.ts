import type { RootStore } from "@/stores/root.store";
import type { SyncEngine } from "@/sync/sync-engine";
import type { FolderRecord } from "@/types/projects/folder-record";
import type { PendingTransaction } from "@/types/sync/transaction";
import { api } from "@/lib/api-client";
import { isConnectivityError } from "@/lib/is-connectivity-error";
import { devError } from "@/sync/dev-log";
import { fractionalAfter } from "@/features/workspace/contents/hierarchy/utils/fractional-index";
import { toJS } from "mobx";

export class FolderMutations {
  private rootStore: RootStore;
  private syncEngine: SyncEngine;

  constructor(rootStore: RootStore, syncEngine: SyncEngine) {
    this.rootStore = rootStore;
    this.syncEngine = syncEngine;
  }

  // ── CREATE ──
  async create(
    data: Omit<FolderRecord, "id" | "createdAt"> & { spaceId: string },
  ): Promise<FolderRecord> {
    const id = crypto.randomUUID();

    // The backend recomputes its own authoritative orderKey server-side on create
    // (FractionalIndex.SafeAfter) and the Delta will overwrite this, but until that round-trip
    // lands, the optimistic record needs a *valid* fractional-indexing key of its own — leaving
    // orderKey undefined broke any reorder attempted in that window (sorts as "" and can't be
    // used as a "between" boundary for other items).
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

    // 1. Optimistic
    this.rootStore.folderStore.upsert(record);

    // 2. Persist
    try {
      await this.rootStore.folderDB!.put(record);
    } catch (err) {
      this.rootStore.folderStore.remove(record.id);
      devError("[FolderMutations] folderDB.put failed:", err);
      throw new Error(
        `Failed to persist folder locally: ${err instanceof Error ? err.message : String(err)}`,
      );
    }

    // CreateFolderCommand wire shape
    const commandPayload = {
      id,
      spaceId: data.spaceId,
      name: data.name,
      color: data.color ?? null,
      icon: data.icon ?? null,
      startDate: data.startDate ?? null,
      dueDate: data.dueDate ?? null,
    };

    // 3. Enqueue transaction
    const tx = await this.syncEngine.transactionQueue.enqueue(
      "C",
      "Folder",
      record.id,
      commandPayload,
      null,
    );

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn("App is offline. Skipping API request. Will sync later.");
      return record;
    }

    try {
      await api.post("/folders/sync", commandPayload, {
        headers: {
          "X-Workspace-Id": this.rootStore.currentWorkspaceId!,
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

  // ── UPDATE (local-only — store + IndexedDB + enqueue, no network call) ──
  // Building block for update(). Call this on every rapid field edit and debounce a
  // syncEngine.flushQueue() trigger instead of debouncing this call — TransactionQueue.squash()
  // already merges multiple pending updates for the same folder into one send. See
  // TaskMutations.updateLocal() for the full rationale.
  async updateLocal(
    folderId: string,
    changes: Partial<FolderRecord>,
  ): Promise<{ previous: FolderRecord; tx: PendingTransaction }> {
    const stored = this.rootStore.folderStore.getById(folderId);
    if (!stored) throw new Error(`Folder ${folderId} not found`);
    const previous = toJS(stored);

    // startDate/dueDate === null means "explicitly clear"; undefined means "not touched" —
    // same convention and same backend requirement as TaskMutations (ProjectFolder.Update()
    // only clears on the boolean flag, ignores a bare null).
    const clearingStartDate = changes.startDate === null;
    const clearingDueDate = changes.dueDate === null;

    const merged = { ...previous, ...changes };

    // 1. Optimistic
    this.rootStore.folderStore.upsert(merged);

    // 2. Persist
    try {
      await this.rootStore.folderDB!.put(merged);
    } catch {
      this.rootStore.folderStore.upsert(previous);
      throw new Error("Failed to persist update locally");
    }

    // UpdateFolderCommand wire shape — only keys the caller actually touched (see
    // TaskMutations.updateLocal for why: squash()'s U+U merge would otherwise clobber an
    // earlier queued update's real change to an untouched field).
    const commandPayload: Record<string, unknown> = {};
    if ("name" in changes) commandPayload.name = changes.name;
    if ("color" in changes) commandPayload.color = changes.color;
    if ("icon" in changes) commandPayload.icon = changes.icon;
    if ("orderKey" in changes) commandPayload.orderKey = changes.orderKey;
    if (clearingStartDate) commandPayload.clearStartDate = true;
    else if ("startDate" in changes)
      commandPayload.startDate = changes.startDate;
    if (clearingDueDate) commandPayload.clearDueDate = true;
    else if ("dueDate" in changes) commandPayload.dueDate = changes.dueDate;
    // Reparenting to a different space (drag-and-drop in the hierarchy sidebar) — no "clear"
    // sentinel needed, a folder always belongs to some space.
    if ("spaceId" in changes) commandPayload.spaceId = changes.spaceId;

    // 3. Enqueue transaction — no network call here
    const tx = await this.syncEngine.transactionQueue.enqueue(
      "U",
      "Folder",
      folderId,
      commandPayload,
      previous as unknown as Record<string, unknown>,
    );

    return { previous, tx };
  }

  // ── UPDATE (immediate — local write + queue + synchronous send) ──
  async update(
    folderId: string,
    changes: Partial<FolderRecord>,
  ): Promise<void> {
    const { previous, tx } = await this.updateLocal(folderId, changes);

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn("App is offline. Skipping API request. Will sync later.");
      return;
    }

    try {
      await api.put(`/folders/sync/${folderId}`, tx.data, {
        headers: {
          "X-Workspace-Id": this.rootStore.currentWorkspaceId!,
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

  // ── DELETE ──
  async delete(folderId: string): Promise<void> {
    const stored = this.rootStore.folderStore.getById(folderId);
    if (!stored) throw new Error(`Folder ${folderId} not found`);
    const previous = toJS(stored);

    // Reparent tasks in this folder to space level — mirrors what the backend does
    for (const task of this.rootStore.taskStore.all.filter(
      (t) => t.folderId === folderId,
    )) {
      const reparented = { ...toJS(task), folderId: null };
      this.rootStore.taskStore.upsert(reparented);
      await this.rootStore.taskDB!.put(reparented);
    }

    // 1. Eager local removal of folder
    this.rootStore.folderStore.remove(folderId);

    // 2. Persist
    try {
      await this.rootStore.folderDB!.delete(folderId);
    } catch {
      this.rootStore.folderStore.upsert(previous);
      throw new Error("Failed to persist delete locally");
    }

    // 3. Enqueue
    const tx = await this.syncEngine.transactionQueue.enqueue(
      "D",
      "Folder",
      folderId,
      { id: folderId },
      previous as unknown as Record<string, unknown>,
    );

    // 4. Synchronous API call
    if (!this.rootStore.isOnline) {
      console.warn("App is offline. Skipping API request. Will sync later.");
      return;
    }

    try {
      await api.delete(`/folders/sync/${folderId}`, {
        headers: {
          "X-Workspace-Id": this.rootStore.currentWorkspaceId!,
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

      this.rootStore.folderStore.upsert(previous);
      await this.rootStore.folderDB!.put(previous);
      await this.syncEngine.transactionQueue.dequeue(tx.id);
      throw err;
    }
  }
}
