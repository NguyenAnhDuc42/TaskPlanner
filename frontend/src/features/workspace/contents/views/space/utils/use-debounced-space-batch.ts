import { useRef, useEffect, useLayoutEffect, useCallback } from "react";
import { store } from "@/store";
import { taskSlice, taskSelectors } from "@/store/entityStore";
import type { BatchUpdateSpaceItemValue } from "../space-api";
import type { TaskRecord } from "@/types/projects";
import { toast } from "sonner";
import { extractErrorMessage } from "@/types/api-error";

type BatchMutate = (args: { spaceId: string; updates: BatchUpdateSpaceItemValue[] }) => { unwrap: () => Promise<void> };

export function useDebouncedSpaceBatch(
  batchUpdate: BatchMutate,
  spaceId: string,
  delay = 2000,
): (update: BatchUpdateSpaceItemValue) => void {
  const pendingRef = useRef<Map<string, BatchUpdateSpaceItemValue>>(new Map());
  // True originals — snapshotted BEFORE optimistic update, cleared after successful save
  const originalsRef = useRef<Map<string, TaskRecord>>(new Map());
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const batchUpdateRef = useRef(batchUpdate);
  const spaceIdRef = useRef(spaceId);
  useLayoutEffect(() => { batchUpdateRef.current = batchUpdate; });
  useLayoutEffect(() => { spaceIdRef.current = spaceId; });

  useEffect(() => () => {
    if (timerRef.current) clearTimeout(timerRef.current);
    const updates = Array.from(pendingRef.current.values());
    pendingRef.current.clear();
    originalsRef.current.clear();
    if (updates.length > 0) {
      batchUpdateRef.current({ spaceId: spaceIdRef.current, updates });
    }
  }, []);

  return useCallback((update: BatchUpdateSpaceItemValue) => {
    // Snapshot BEFORE the optimistic update — only on first touch per item per batch window
    if (!originalsRef.current.has(update.id)) {
      const task = taskSelectors.selectById(store.getState(), update.id);
      if (task) originalsRef.current.set(update.id, task);
    }

    // Merge pending updates (last-write-wins per field)
    const existing = pendingRef.current.get(update.id);
    pendingRef.current.set(update.id, { ...existing, ...update });

    // Optimistic update immediately
    store.dispatch(taskSlice.actions.upsert(update as Partial<TaskRecord> & { id: string }));

    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(async () => {
      const updates = Array.from(pendingRef.current.values());
      const originals = Array.from(originalsRef.current.values());
      pendingRef.current.clear();
      originalsRef.current.clear();
      timerRef.current = null;

      if (updates.length === 0) return;

      try {
        await batchUpdateRef.current({ spaceId: spaceIdRef.current, updates }).unwrap();
      } catch (err) {
        // Revert to true pre-edit state
        if (originals.length > 0) {
          store.dispatch(taskSlice.actions.upsertMany(originals));
        }
        toast.error(extractErrorMessage(err, "Failed to save changes. Reverted."));
      }
    }, delay);
  }, [delay]);
}
