import type { RootStore } from "@/stores/root.store";
import type { TaskRecord, SpaceRecord, FolderRecord, CommentRecord, AssigneeRecord } from "@/types/projects";
import type { DocumentRecord } from "@/types/document";
import type { DocumentBlockRecord } from "@/types/document/document-block-record";
import type { Status } from "@/types/status";
import type { WorkspaceRecord } from "@/types/workspace/workspace-record";
import type { MemberRecord } from "@/types/workspace/member-record";
import type { DeltaPayload, SyncEntityType } from "@/types/sync/delta";

type EntityApplier = {
  upsert: (data: Record<string, unknown>) => void;
  remove: (id: string) => void;
  dbPut: (data: Record<string, unknown>) => Promise<void>;
  dbDelete: (id: string) => Promise<void>;
};

function getEntityApplier(
  rootStore: RootStore,
  entityType: SyncEntityType,
  cancelByEntityId?: (id: string) => Promise<void>,
): EntityApplier | null {
  switch (entityType) {
    case "Workspace":
      return {
        upsert: (data) => rootStore.workspaceStore.upsert(data as unknown as WorkspaceRecord),
        remove: (id) => rootStore.workspaceStore.remove(id),
        dbPut: (data) => rootStore.workspaceDB?.put(data as unknown as WorkspaceRecord) ?? Promise.resolve(),
        dbDelete: (id) => rootStore.workspaceDB?.delete(id) ?? Promise.resolve(),
      };

    case "Space":
      return {
        upsert: (data) => rootStore.spaceStore.upsert(data as unknown as SpaceRecord),
        // Cascade: when a space is deleted, all its children are also gone.
        // dbDelete runs first (awaited), stores still have the entities at that point.
        remove: (id) => {
          rootStore.folderStore.all.filter(f => f.spaceId === id).forEach(f => rootStore.folderStore.remove(f.id))
          rootStore.taskStore.all.filter(t => t.spaceId === id).forEach(t => rootStore.taskStore.remove(t.id))
          rootStore.statusStore.all.filter((s: Status) => s.spaceId === id).forEach((s: Status) => rootStore.statusStore.remove(s.id))
          rootStore.spaceStore.remove(id)
        },
        dbPut: (data) => rootStore.spaceDB!.put(data as unknown as SpaceRecord),
        dbDelete: async (id) => {
          const folderIds = rootStore.folderStore.all.filter(f => f.spaceId === id).map(f => f.id)
          const taskIds = rootStore.taskStore.all.filter(t => t.spaceId === id).map(t => t.id)
          const statusIds = rootStore.statusStore.all.filter((s: Status) => s.spaceId === id).map((s: Status) => s.id)
          // Cancel any queued ops for children — prevents queue from jamming on 404s after another user deletes this space
          if (cancelByEntityId) await Promise.all([...folderIds, ...taskIds].map(cid => cancelByEntityId(cid)))
          await Promise.all([
            rootStore.spaceDB!.delete(id),
            ...folderIds.map(fid => rootStore.folderDB!.delete(fid)),
            ...taskIds.map(tid => rootStore.taskDB!.delete(tid)),
            ...statusIds.map(sid => rootStore.statusDB!.delete(sid)),
          ])
        },
      };

    case "Folder":
      return {
        upsert: (data) => rootStore.folderStore.upsert(data as unknown as FolderRecord),
        remove: (id) => rootStore.folderStore.remove(id),
        dbPut: (data) => rootStore.folderDB!.put(data as unknown as FolderRecord),
        dbDelete: (id) => rootStore.folderDB!.delete(id),
      };

    case "Task":
      return {
        upsert: (data) => rootStore.taskStore.upsert(data as unknown as TaskRecord),
        remove: (id) => rootStore.taskStore.remove(id),
        dbPut: (data) => rootStore.taskDB!.put(data as unknown as TaskRecord),
        dbDelete: (id) => rootStore.taskDB!.delete(id),
      };

    case "Document":
      return {
        upsert: (data) => rootStore.documentStore.upsert(data as unknown as DocumentRecord),
        remove: (id) => rootStore.documentStore.remove(id),
        dbPut: (data) => rootStore.documentDB!.put(data as unknown as DocumentRecord),
        dbDelete: (id) => rootStore.documentDB!.delete(id),
      };

    case "DocumentBlock":
      return {
        upsert: (data) => rootStore.documentBlockStore.upsert(data as unknown as DocumentBlockRecord),
        remove: (id) => rootStore.documentBlockStore.remove(id),
        dbPut: (data) => rootStore.documentBlockDB!.put(data as unknown as DocumentBlockRecord),
        dbDelete: (id) => rootStore.documentBlockDB!.delete(id),
      };

    case "Status":
      return {
        upsert: (data) => rootStore.statusStore.upsert(data as unknown as Status),
        remove: (id) => rootStore.statusStore.remove(id),
        dbPut: (data) => rootStore.statusDB!.put(data as unknown as Status),
        dbDelete: (id) => rootStore.statusDB!.delete(id),
      };

    case "Comment":
      return {
        upsert: (data) => rootStore.commentStore.upsert(data as unknown as CommentRecord),
        remove: (id) => rootStore.commentStore.remove(id),
        dbPut: (data) => rootStore.commentDB!.put(data as unknown as CommentRecord),
        dbDelete: (id) => rootStore.commentDB!.delete(id),
      };

    case "Assignee":
      return {
        upsert: (data) => rootStore.assigneeStore.upsert(data as unknown as AssigneeRecord),
        remove: (id) => rootStore.assigneeStore.remove(id),
        dbPut: (data) => rootStore.assigneeDB!.put(data as unknown as AssigneeRecord),
        dbDelete: (id) => rootStore.assigneeDB!.delete(id),
      };

    case "Member":
      return {
        upsert: (data) => rootStore.memberStore.upsert(data as unknown as MemberRecord),
        remove: (id) => rootStore.memberStore.remove(id),
        dbPut: (data) => rootStore.memberDB!.put(data as unknown as MemberRecord),
        dbDelete: (id) => rootStore.memberDB!.delete(id),
      };

    default:
      console.warn(`Unknown entity: ${entityType}`);
      return null;
  }
}

export async function applyDelta(
  rootStore: RootStore,
  delta: DeltaPayload,
  cancelByEntityId?: (id: string) => Promise<void>,
): Promise<void> {
  const applier = getEntityApplier(rootStore, delta.entityType, cancelByEntityId);
  if (!applier) return;

  switch (delta.action) {
    case "C":
    case "U": {
      // Guard: if the parent space was already cascade-deleted, discard this echo.
      // This covers the race where a child's in-flight mutation echoes back after
      // the Space D cascade already removed everything from the store.
      const spaceId = delta.data.spaceId as string | undefined
      if (spaceId && (delta.entityType === "Task" || delta.entityType === "Folder" || delta.entityType === "Status")) {
        if (!rootStore.spaceStore.getById(spaceId)) {
          await applier.dbDelete(delta.entityId)
          break
        }
      }
      await applier.dbPut(delta.data);
      applier.upsert(delta.data);
      break;
    }
    case "D":
      await cancelByEntityId?.(delta.entityId);
      await applier.dbDelete(delta.entityId);
      applier.remove(delta.entityId);
      break;
  }

  if (delta.clientTraceId) {
    await rootStore.transactionDB!.dequeue(delta.clientTraceId);
  } else {
    const pendingTxs = await rootStore.transactionDB!.getByEntity(
      delta.entityType,
      delta.entityId,
    );
    for (const tx of pendingTxs) {
      if (tx.action === delta.action) {
        await rootStore.transactionDB!.dequeue(tx.id);
      }
    }
  }

  await rootStore.metadataDB!.setLastSyncId(delta.syncId);
}

export async function applyDeltaBatch(
  rootStore: RootStore,
  deltas: DeltaPayload[],
  cancelByEntityId?: (id: string) => Promise<void>,
): Promise<void> {
  const sorted = [...deltas].sort((a, b) => a.syncId - b.syncId);
  for (const delta of sorted) {
    await applyDelta(rootStore, delta, cancelByEntityId);
  }
}
