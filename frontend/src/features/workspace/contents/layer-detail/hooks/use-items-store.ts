import { create } from "zustand";
import type { BatchUpdateItemDto, LayerItem } from "../layer-detail-types";
import { batchUpdateItems } from "../layer-detail-api";
import { inferPriorityFromNeighbors } from "../views/shared/item-dnd-helpers";
import { fractionalBetween, fractionalAfter } from "../../hierarchy/utils/fractional-index";
import { Priority, prioritySort } from "@/types/priority";
import { queryClient } from "@/main";
import { workspaceKeys } from "@/features/main/query-keys";

export type { BatchUpdateItemDto };

interface ItemsStoreState {
  columns: Record<string, LayerItem[]>;
  queue: Record<string, BatchUpdateItemDto>;
  isSaving: boolean;
  debounceTimeout: ReturnType<typeof setTimeout> | null;
  layerType: "space" | "folder" | null;
  layerId: string | null;
  isInitialized: boolean;
  
  // Actions
  setLayerInfo: (layerType: "space" | "folder", layerId: string) => void;
  initializeColumns: (columns: Record<string, LayerItem[]>) => void;
  setColumns: (columns: Record<string, LayerItem[]>) => void;
  // Live drag preview — updates columns immediately, no API call
  previewMove: (params: { activeId: string; fromColId: string; toColId: string; toIndex: number }) => void;
  moveItem: (workspaceId: string, params: {
    activeId: string;
    targetStatusId: string | undefined;
    targetIndex: number;
    previousItemOrderKey: string | undefined;
    nextItemOrderKey: string | undefined;
  }) => void;
  changePriority: (workspaceId: string, itemId: string, priority: Priority) => void;
  
  addUpdate: (workspaceId: string, update: BatchUpdateItemDto) => void;
  flushQueue: (workspaceId: string) => Promise<void>;
  clearQueue: () => void;
}

/** True while items are queued OR the API call is in-flight */
export const selectHasPendingUpdates = (state: ItemsStoreState) =>
  Object.keys(state.queue).length > 0 || state.isSaving;

export const useItemsStore = create<ItemsStoreState>((set, get) => ({
  columns: {},
  queue: {},
  isSaving: false,
  debounceTimeout: null,
  layerType: null,
  layerId: null,
  isInitialized: false,

  setLayerInfo: (layerType, layerId) => set({ layerType, layerId, isInitialized: false }),
  initializeColumns: (columns) => set({ columns, isInitialized: true }),
  setColumns: (columns) => set({ columns }),

  previewMove: ({ activeId, fromColId, toColId, toIndex }) => {
    set((state) => {
      const srcItems = [...(state.columns[fromColId] ?? [])];
      const activeIndex = srcItems.findIndex((i) => i.id === activeId);
      if (activeIndex === -1) return state;
      const [movedItem] = srcItems.splice(activeIndex, 1);

      if (fromColId === toColId) {
        srcItems.splice(toIndex, 0, movedItem);
        return { columns: { ...state.columns, [fromColId]: srcItems } };
      }

      const dstItems = [...(state.columns[toColId] ?? [])];
      dstItems.splice(toIndex, 0, {
        ...movedItem,
        statusId: toColId === "unclassified" ? undefined : toColId,
      });
      return {
        columns: {
          ...state.columns,
          [fromColId]: srcItems,
          [toColId]: dstItems,
        },
      };
    });
  },

  moveItem: (workspaceId, { activeId, targetStatusId, targetIndex, previousItemOrderKey, nextItemOrderKey }) => {
    set((state) => {
      const srcColId = Object.keys(state.columns).find((key) =>
        state.columns[key].some((item) => item.id === activeId)
      ) ?? "unclassified";
      const dstColId = targetStatusId ?? "unclassified";

      const srcItems = [...(state.columns[srcColId] ?? [])];
      const dstItems = srcColId === dstColId ? srcItems : [...(state.columns[dstColId] ?? [])];

      const activeItemIndex = srcItems.findIndex((item) => item.id === activeId);
      if (activeItemIndex === -1) return state;
      const activeItem = srcItems[activeItemIndex];

      const newPriority = inferPriorityFromNeighbors({
        activeItem,
        targetIndex,
        dstItems,
      });

      let calculatedOrderKey = fractionalBetween(previousItemOrderKey, nextItemOrderKey);
      if (previousItemOrderKey && nextItemOrderKey && previousItemOrderKey >= nextItemOrderKey) {
        calculatedOrderKey = fractionalAfter(previousItemOrderKey);
      }

      const updatedItem = { 
        ...activeItem, 
        statusId: targetStatusId, 
        priority: newPriority, 
        orderKey: calculatedOrderKey 
      };

      // Update local columns layout
      const nextColumns = { ...state.columns };
      srcItems.splice(activeItemIndex, 1);
      
      if (srcColId === dstColId) {
        srcItems.splice(targetIndex, 0, updatedItem);
        nextColumns[srcColId] = srcItems;
      } else {
        dstItems.splice(targetIndex, 0, updatedItem);
        nextColumns[srcColId] = srcItems;
        nextColumns[dstColId] = dstItems;
      }

      // Enqueue API update
      const updateDto: BatchUpdateItemDto = {
        id: activeItem.id,
        type: activeItem.__type === "task" ? "ProjectTask" : "ProjectFolder",
        statusId: targetStatusId ?? "unclassified",
        priority: newPriority,
        orderKey: calculatedOrderKey,
        previousItemOrderKey,
        nextItemOrderKey,
      };

      const existing = state.queue[activeItem.id];
      const merged = { ...existing, ...updateDto };

      const nextQueue = { ...state.queue, [activeItem.id]: merged };

      let timeout = state.debounceTimeout;
      if (timeout) clearTimeout(timeout);
      timeout = setTimeout(() => { get().flushQueue(workspaceId); }, 1500);

      return {
        columns: nextColumns,
        queue: nextQueue,
        debounceTimeout: timeout,
      };
    });
  },

  changePriority: (workspaceId, itemId, priority) => {
    set((state) => {
      let targetColId = "";
      let targetItem: LayerItem | null = null;
      for (const colId of Object.keys(state.columns)) {
        const item = state.columns[colId].find((i) => i.id === itemId);
        if (item) {
          targetColId = colId;
          targetItem = item;
          break;
        }
      }

      if (!targetItem) return state;

      const nextColumns = { ...state.columns };
      const list = [...nextColumns[targetColId]];
      
      const idx = list.findIndex((i) => i.id === itemId);
      const updatedItem = { ...targetItem, priority };
      list[idx] = updatedItem;
      nextColumns[targetColId] = list.sort(prioritySort);

      const updateDto: BatchUpdateItemDto = {
        id: targetItem.id,
        type: targetItem.__type === "task" ? "ProjectTask" : "ProjectFolder",
        priority,
      };

      const existing = state.queue[targetItem.id];
      const merged = { ...existing, ...updateDto };
      if (existing) {
        if (updateDto.statusId === undefined) merged.statusId = existing.statusId;
        if (updateDto.orderKey === undefined) merged.orderKey = existing.orderKey;
      }

      const nextQueue = { ...state.queue, [targetItem.id]: merged };

      let timeout = state.debounceTimeout;
      if (timeout) clearTimeout(timeout);
      timeout = setTimeout(() => { get().flushQueue(workspaceId); }, 1500);

      return {
        columns: nextColumns,
        queue: nextQueue,
        debounceTimeout: timeout,
      };
    });
  },

  addUpdate: (workspaceId, update) => {
    set((state) => {
      const existing = state.queue[update.id];
      const merged = { ...existing, ...update };
      
      if (update.statusId === undefined && existing) merged.statusId = existing.statusId;
      if (update.priority === undefined && existing) merged.priority = existing.priority;
      if (update.orderKey === undefined && existing) merged.orderKey = existing.orderKey;

      const nextQueue = { ...state.queue, [update.id]: merged };

      let timeout = state.debounceTimeout;
      if (timeout) clearTimeout(timeout);
      timeout = setTimeout(() => { get().flushQueue(workspaceId); }, 1500);

      return {
        queue: nextQueue,
        debounceTimeout: timeout,
      };
    });
  },

  flushQueue: async (workspaceId) => {
    const { queue, isSaving } = get();
    const updates = Object.values(queue);
    if (updates.length === 0 || isSaving) return;

    set({ isSaving: true });

    const timeout = get().debounceTimeout;
    if (timeout) clearTimeout(timeout);
    set({ debounceTimeout: null });

    try {
      await batchUpdateItems(workspaceId, updates);

      // Force React Query to download the newly saved orderKeys for THIS specific view and WAIT for it to finish.
      const { layerType, layerId } = get();
      if (layerType && layerId) {
        await queryClient.invalidateQueries({
          queryKey: [...workspaceKeys.all, layerType, layerId, "items"],
        });
      }

      set((state) => {
        const nextQueue = { ...state.queue };
        for (const update of updates) {
          delete nextQueue[update.id];
        }
        return { queue: nextQueue, isSaving: false };
      });
    } catch (error) {
      console.error("Failed to batch update items:", error);
      
      // ROLLBACK: If API fails, we explicitly mark uninitialized so it forcefully pulls the correct viewData back into the UI
      set({ isSaving: false, isInitialized: false });
    }
  },

  clearQueue: () => {
    const timeout = get().debounceTimeout;
    if (timeout) clearTimeout(timeout);
    set({ queue: {}, debounceTimeout: null, isSaving: false });
  },
}));
