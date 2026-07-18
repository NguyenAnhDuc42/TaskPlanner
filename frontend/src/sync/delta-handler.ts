import type { WorkspaceRootStore } from "@/stores/workspace-root.store";
import { getActiveRootStore } from "@/stores/root.store";
import type { TaskRecord, SpaceRecord, FolderRecord, CommentRecord, AssigneeRecord, DocumentRecord } from "@/types/projects";
import type { DocumentBlockRecord } from "@/types/document/document-block-record";
import type { Status } from "@/types/status";
import type { WorkspaceRecord } from "@/types/workspace/workspace-record";
import type { MemberRecord } from "@/types/workspace/member-record";
import type { DeltaPayload, SyncEntityType } from "@/types/sync/delta";

type EntityApplier = {
  getExisting: (id: string) => Record<string, unknown> | undefined;
  upsert: (data: Record<string, unknown>) => void;
  remove: (id: string) => void;
  dbPut: (data: Record<string, unknown>) => Promise<void>;
  dbDelete: (id: string) => Promise<void>;
};

function getEntityApplier(
  rootStore: WorkspaceRootStore,
  entityType: SyncEntityType,
  cancelByEntityIds?: (ids: string[]) => Promise<void>,
): EntityApplier | null {
  switch (entityType) {
    case "Workspace":
      return {
        getExisting: (id) => getActiveRootStore()?.workspaceStore.getById(id) as Record<string, unknown> | undefined,
        upsert: (data) => getActiveRootStore()?.workspaceStore.upsert(data as unknown as WorkspaceRecord),
        remove: (id) => getActiveRootStore()?.workspaceStore.remove(id),
        dbPut: (data) => getActiveRootStore()?.workspaceDB?.put(data as unknown as WorkspaceRecord) ?? Promise.resolve(),
        dbDelete: (id) => getActiveRootStore()?.workspaceDB?.delete(id) ?? Promise.resolve(),
      };

    case "Space":
      return {
        getExisting: (id) => rootStore.spaceStore.getById(id) as Record<string, unknown> | undefined,
        upsert: (data) => rootStore.spaceStore.upsert(data as unknown as SpaceRecord),
        remove: (id) => {
          rootStore.folderStore.getBySpace(id).map(f => f.id).forEach(fid => rootStore.folderStore.remove(fid))
          rootStore.taskStore.getBySpace(id).map(t => t.id).forEach(tid => rootStore.taskStore.remove(tid))
          rootStore.statusStore.getBySpace(id).map((s: Status) => s.id).forEach(sid => rootStore.statusStore.remove(sid))
          const documentIds = rootStore.documentStore.all.filter(d => d.spaceId === id).map(d => d.id)
          documentIds.forEach(did => rootStore.documentStore.remove(did))
          documentIds.forEach(did => rootStore.documentBlockStore.removeByDocument(did))
          rootStore.spaceStore.remove(id)
        },
        dbPut: (data) => rootStore.spaceDB!.put(data as unknown as SpaceRecord),
        dbDelete: async (id) => {
          const folderIds = rootStore.folderStore.getBySpace(id).map(f => f.id)
          const taskIds = rootStore.taskStore.getBySpace(id).map(t => t.id)
          const statusIds = rootStore.statusStore.getBySpace(id).map((s: Status) => s.id)
          const documentIds = rootStore.documentStore.all.filter(d => d.spaceId === id).map(d => d.id)
          // Cancel any queued ops for children — prevents queue from jamming on 404s after another user deletes this space
          if (cancelByEntityIds) await cancelByEntityIds([...folderIds, ...taskIds, ...documentIds])
          await Promise.all([
            rootStore.spaceDB!.delete(id),
            ...folderIds.map(fid => rootStore.folderDB!.delete(fid)),
            ...taskIds.map(tid => rootStore.taskDB!.delete(tid)),
            ...statusIds.map(sid => rootStore.statusDB!.delete(sid)),
            rootStore.documentDB!.deleteMany(documentIds),
            rootStore.documentBlockDB!.deleteByDocumentIds(documentIds),
          ])
        },
      };

    case "Folder":
      return {
        getExisting: (id) => rootStore.folderStore.getById(id) as Record<string, unknown> | undefined,
        upsert: (data) => rootStore.folderStore.upsert(data as unknown as FolderRecord),
        remove: (id) => rootStore.folderStore.remove(id),
        dbPut: (data) => rootStore.folderDB!.put(data as unknown as FolderRecord),
        dbDelete: (id) => rootStore.folderDB!.delete(id),
      };

    case "Document":
      return {
        getExisting: (id) => rootStore.documentStore.getById(id) as Record<string, unknown> | undefined,
        upsert: (data) => rootStore.documentStore.upsert(data as unknown as DocumentRecord),
        // A Document delete cascades its whole subtree — unlike Folder, which only ever touches
        // a single row. The client already has the full descendant list locally (the tree is
        // fully hydrated), so it can cascade a delta arriving from another client the same way
        // DocumentMutations.delete cascades a locally-initiated one.
        remove: (id) => {
          const descendantIds = rootStore.documentStore.getDescendantIds(id)
          rootStore.documentStore.removeMany(descendantIds)
          descendantIds.forEach(did => rootStore.documentBlockStore.removeByDocument(did))
        },
        dbPut: (data) => rootStore.documentDB!.put(data as unknown as DocumentRecord),
        dbDelete: async (id) => {
          const descendantIds = rootStore.documentStore.getDescendantIds(id)
          if (cancelByEntityIds) await cancelByEntityIds(descendantIds)
          await Promise.all([
            rootStore.documentDB!.deleteMany(descendantIds),
            rootStore.documentBlockDB!.deleteByDocumentIds(descendantIds),
          ])
        },
      };

    case "Task":
      return {
        getExisting: (id) => rootStore.taskStore.getById(id) as Record<string, unknown> | undefined,
        upsert: (data) => rootStore.taskStore.upsert(data as unknown as TaskRecord),
        remove: (id) => rootStore.taskStore.remove(id),
        dbPut: (data) => rootStore.taskDB!.put(data as unknown as TaskRecord),
        dbDelete: (id) => rootStore.taskDB!.delete(id),
      };

    case "DocumentBlock":
      return {
        getExisting: (id) => rootStore.documentBlockStore.getById(id) as Record<string, unknown> | undefined,
        upsert: (data) => rootStore.documentBlockStore.upsert(data as unknown as DocumentBlockRecord),
        remove: (id) => rootStore.documentBlockStore.remove(id),
        dbPut: (data) => rootStore.documentBlockDB!.put(data as unknown as DocumentBlockRecord),
        dbDelete: (id) => rootStore.documentBlockDB!.delete(id),
      };

    case "Status":
      return {
        getExisting: (id) => rootStore.statusStore.getById(id) as Record<string, unknown> | undefined,
        upsert: (data) => rootStore.statusStore.upsert(data as unknown as Status),
        remove: (id) => rootStore.statusStore.remove(id),
        dbPut: (data) => rootStore.statusDB!.put(data as unknown as Status),
        dbDelete: (id) => rootStore.statusDB!.delete(id),
      };

    case "Comment":
      return {
        getExisting: (id) => rootStore.commentStore.getById(id) as Record<string, unknown> | undefined,
        upsert: (data) => rootStore.commentStore.upsert(data as unknown as CommentRecord),
        remove: (id) => rootStore.commentStore.remove(id),
        dbPut: (data) => rootStore.commentDB!.put(data as unknown as CommentRecord),
        dbDelete: (id) => rootStore.commentDB!.delete(id),
      };

    case "Assignee":
      return {
        getExisting: (id) => rootStore.assigneeStore.getById(id) as Record<string, unknown> | undefined,
        upsert: (data) => rootStore.assigneeStore.upsert(data as unknown as AssigneeRecord),
        remove: (id) => rootStore.assigneeStore.remove(id),
        dbPut: (data) => rootStore.assigneeDB!.put(data as unknown as AssigneeRecord),
        dbDelete: (id) => rootStore.assigneeDB!.delete(id),
      };

    case "Member":
      return {
        getExisting: (id) => rootStore.memberStore.getById(id) as Record<string, unknown> | undefined,
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
  rootStore: WorkspaceRootStore,
  delta: DeltaPayload,
  cancelByEntityIds?: (ids: string[]) => Promise<void>,
): Promise<void> {
  await applyDeltaCore(rootStore, delta, cancelByEntityIds);
  await rootStore.metadataDB!.setLastSyncId(delta.syncId);
}

async function applyDeltaCore(
  rootStore: WorkspaceRootStore,
  delta: DeltaPayload,
  cancelByEntityIds?: (ids: string[]) => Promise<void>,
): Promise<void> {
  const applier = getEntityApplier(rootStore, delta.entityType, cancelByEntityIds);
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
      const existing = applier.getExisting(delta.entityId);
      const merged = existing ? { ...existing, ...delta.data } : delta.data;
      await applier.dbPut(merged);
      applier.upsert(merged);
      break;
    }
    case "D":
      await cancelByEntityIds?.([delta.entityId]);
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
}

export async function applyDeltaBatch(
  rootStore: WorkspaceRootStore,
  deltas: DeltaPayload[],
  cancelByEntityIds?: (ids: string[]) => Promise<void>,
): Promise<void> {
  if (deltas.length === 0) return;
  const sorted = [...deltas].sort((a, b) => a.syncId - b.syncId);
  for (const delta of sorted) {
    await applyDeltaCore(rootStore, delta, cancelByEntityIds);
  }
  // One lastSyncId write for the whole batch — see applyDeltaCore.
  await rootStore.metadataDB!.setLastSyncId(sorted[sorted.length - 1].syncId);
}
