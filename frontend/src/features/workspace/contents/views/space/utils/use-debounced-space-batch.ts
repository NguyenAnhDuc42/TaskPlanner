import { useRef, useEffect, useLayoutEffect, useCallback } from "react";
import { store } from "@/store";
import { taskSlice, folderSlice } from "@/store/entityStore";
import type { BatchUpdateSpaceItemValue } from "../space-api";
import { EntityLayerType } from "@/types/entity-layer-type";
import type { TaskRecord, FolderRecord } from "@/types/projects";

/**
 * Accumulates space batch updates and fires one API call after `delay` ms of
 * inactivity. Each call immediately applies an optimistic store update so the
 * UI feels instant regardless of the debounce window.
 *
 * Multiple changes to the same item are merged (last-write-wins per field)
 * so rapid edits collapse into a single payload.
 */
export function useDebouncedSpaceBatch(
  batchUpdate: (args: { spaceId: string; updates: BatchUpdateSpaceItemValue[] }) => void,
  spaceId: string,
  delay = 2000,
): (update: BatchUpdateSpaceItemValue) => void {
  const pendingRef = useRef<Map<string, BatchUpdateSpaceItemValue>>(new Map());
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Keep latest refs so the scheduled flush always uses current values
  const batchUpdateRef = useRef(batchUpdate);
  const spaceIdRef = useRef(spaceId);
  useLayoutEffect(() => { batchUpdateRef.current = batchUpdate; });
  useLayoutEffect(() => { spaceIdRef.current = spaceId; });

  // On unmount (navigation away): cancel the timer and flush any pending updates immediately
  useEffect(() => () => {
    if (timerRef.current) clearTimeout(timerRef.current);
    const updates = Array.from(pendingRef.current.values());
    pendingRef.current.clear();
    if (updates.length > 0) {
      batchUpdateRef.current({ spaceId: spaceIdRef.current, updates });
    }
  }, []);

  return useCallback((update: BatchUpdateSpaceItemValue) => {
    // Merge with any pending update for this item — last-write-wins per field
    const existing = pendingRef.current.get(update.id);
    pendingRef.current.set(update.id, { ...existing, ...update });

    // Optimistic store update immediately so UI reflects the change at once
    if (update.type === EntityLayerType.ProjectTask) {
      store.dispatch(taskSlice.actions.upsert(update as Partial<TaskRecord> & { id: string }));
    } else {
      store.dispatch(folderSlice.actions.upsert(update as Partial<FolderRecord> & { id: string }));
    }

    // Reset the debounce — only the last change in the window triggers the API
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => {
      const updates = Array.from(pendingRef.current.values());
      pendingRef.current.clear();
      if (updates.length > 0) {
        batchUpdateRef.current({ spaceId: spaceIdRef.current, updates });
      }
    }, delay);
  }, [delay]);
}
