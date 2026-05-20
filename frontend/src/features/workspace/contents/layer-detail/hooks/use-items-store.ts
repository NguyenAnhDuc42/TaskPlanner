import { create } from "zustand";
import type { BatchUpdateItemDto } from "../layer-detail-types";
import { batchUpdateItems } from "../layer-detail-api";

export type { BatchUpdateItemDto };

interface ItemsStoreState {
  queue: Record<string, BatchUpdateItemDto>;
  isSaving: boolean;
  debounceTimeout: ReturnType<typeof setTimeout> | null;
  
  // Actions
  addUpdate: (workspaceId: string, update: BatchUpdateItemDto) => void;
  flushQueue: (workspaceId: string) => Promise<void>;
  clearQueue: () => void;
}

/** True while items are queued OR the API call is in-flight */
export const selectHasPendingUpdates = (state: ItemsStoreState) =>
  Object.keys(state.queue).length > 0 || state.isSaving;

export const useItemsStore = create<ItemsStoreState>((set, get) => ({
  queue: {},
  isSaving: false,
  debounceTimeout: null,

  addUpdate: (workspaceId: string, update: BatchUpdateItemDto) => {
    set((state) => {
      const existing = state.queue[update.id];
      const merged = {
        ...existing,
        ...update,
      };
      
      // Preserve existing fields when update doesn't explicitly set them
      if (update.statusId === undefined && existing) merged.statusId = existing.statusId;
      if (update.priority === undefined && existing) merged.priority = existing.priority;
      if (update.orderKey === undefined && existing) merged.orderKey = existing.orderKey;

      const nextQueue = {
        ...state.queue,
        [update.id]: merged,
      };

      // Handle debounced saving
      let timeout = state.debounceTimeout;
      if (timeout) clearTimeout(timeout);

      timeout = setTimeout(() => {
        get().flushQueue(workspaceId);
      }, 1500);

      return {
        queue: nextQueue,
        debounceTimeout: timeout,
      };
    });
  },

  flushQueue: async (workspaceId: string) => {
    const { queue, isSaving } = get();
    const updates = Object.values(queue);
    if (updates.length === 0 || isSaving) return;

    set({ isSaving: true });

    const timeout = get().debounceTimeout;
    if (timeout) clearTimeout(timeout);
    set({ debounceTimeout: null });

    try {
      await batchUpdateItems(workspaceId, updates);

      set((state) => {
        const nextQueue = { ...state.queue };
        for (const update of updates) {
          delete nextQueue[update.id];
        }
        return { queue: nextQueue, isSaving: false };
      });
    } catch (error) {
      console.error("Failed to batch update items:", error);
      set({ isSaving: false });
    }
  },

  clearQueue: () => {
    const timeout = get().debounceTimeout;
    if (timeout) clearTimeout(timeout);
    set({ queue: {}, debounceTimeout: null, isSaving: false });
  },
}));
