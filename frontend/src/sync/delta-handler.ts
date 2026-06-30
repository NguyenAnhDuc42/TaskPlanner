import type { RootStore } from "@/stores/root.store";
import type { TaskRecord } from "@/types/projects";
import type { DocumentRecord } from "@/types/document";
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
): EntityApplier | null {
  switch (entityType) {
    case "Task":
      return {
        upsert: (data) =>
          rootStore.taskStore.upsert(data as unknown as TaskRecord),
        remove: (id) => rootStore.taskStore.remove(id),
        dbPut: (data) => rootStore.taskDB!.put(data as unknown as TaskRecord),
        dbDelete: (id) => rootStore.taskDB!.delete(id),
      };
    case "Document":
      return {
        upsert: (data) =>
          rootStore.documentStore.upsert(data as unknown as DocumentRecord),
        remove: (id) => rootStore.documentStore.remove(id),
        dbPut: (data) => rootStore.documentDB!.put(data as unknown as DocumentRecord),
        dbDelete: (id) => rootStore.documentDB!.delete(id),
      };
    //more case
    default:
      console.warn(`Unknown entity: ${entityType}`);
      return null;
  }
}

export async function applyDelta(
  rootStore: RootStore,
  delta: DeltaPayload,
): Promise<void> {
  const applier = getEntityApplier(rootStore, delta.entityType);
  if (!applier) return;

  switch (delta.action) {
    case "C":
    case "U":
      await applier.dbPut(delta.data);
      applier.upsert(delta.data);
      break;
    case "D":
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

  // Update lastSyncId
  await rootStore.metadataDB!.setLastSyncId(delta.syncId);
}

export async function applyDeltaBatch(
  rootStore: RootStore,
  deltas: DeltaPayload[],
): Promise<void> {
  // Sort by syncId to apply in order
  const sorted = [...deltas].sort((a, b) => a.syncId - b.syncId);
  for (const delta of sorted) {
    await applyDelta(rootStore, delta);
  }
}
