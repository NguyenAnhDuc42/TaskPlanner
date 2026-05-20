import { useState, useEffect, useMemo, useRef } from "react";
import { useNavigate, useParams } from "@tanstack/react-router";
import { useQueryClient } from "@tanstack/react-query";
import { workspaceKeys } from "@/features/main/query-keys";
import type { FolderItemDto, LayerItem, TaskItemDto, TaskViewData } from "../../layer-detail-types";
import { buildColumns } from "./space-dnd-helpers";
import {
  Priority,
  prioritySort,
} from "@/types/priority";
import { useItemsStore, selectHasPendingUpdates } from "../../hooks/use-items-store";
import { EntityLayerType } from "@/types/entity-layer-type";
import { UnifiedBoardView } from "../unified-board-view";
import { UnifiedListView } from "../unified-list-view";
import { inferPriorityFromNeighbors } from "../shared/item-dnd-helpers";

interface SpaceItemsViewProps {
  viewData: TaskViewData;
  spaceId: string;
  viewMode: "board" | "list";
}

export function SpaceItemsView({
  viewData,
  spaceId,
  viewMode,
}: SpaceItemsViewProps) {
  const navigate = useNavigate();
  const { workspaceId } = useParams({ strict: false }) as unknown as {
    workspaceId: string;
  };
  const queryClient = useQueryClient();

  const [columns, setColumns] = useState<Record<string, LayerItem[]>>(() =>
    buildColumns(viewData),
  );

  const columnsRef = useRef<Record<string, LayerItem[]>>(columns);
  columnsRef.current = columns;

  // Block viewData sync while ANY update is queued or in-flight
  const hasPending = useItemsStore(selectHasPendingUpdates);
  const prevHasPendingRef = useRef(hasPending);
  const [isRefetchingAfterSave, setIsRefetchingAfterSave] = useState(false);
  const suppressViewDataSyncRef = useRef(false);
  const itemsQueryKey = useMemo(
    () => [...workspaceKeys.all, "space", spaceId, "items"],
    [spaceId],
  );

  // When a batch save completes, refetch server data so orderKey-based sorting matches
  useEffect(() => {
    const prevHasPending = prevHasPendingRef.current;
    prevHasPendingRef.current = hasPending;

    if (prevHasPending && !hasPending) {
      setIsRefetchingAfterSave(true);
      (async () => {
        try {
          await queryClient.invalidateQueries({ queryKey: itemsQueryKey });
        } finally {
          setIsRefetchingAfterSave(false);
        }
      })();
    }
  }, [hasPending, queryClient, itemsQueryKey]);

  useEffect(() => {
    // As soon as a drop enqueues an update, stop syncing from viewData
    if (hasPending) suppressViewDataSyncRef.current = true;

    // Once everything is saved and refetched, allow normal syncing again
    if (!hasPending && !isRefetchingAfterSave) suppressViewDataSyncRef.current = false;
  }, [hasPending, isRefetchingAfterSave]);

  useEffect(() => {
    if (hasPending || isRefetchingAfterSave || suppressViewDataSyncRef.current) return;
    setColumns(buildColumns(viewData));
  }, [viewData, hasPending, isRefetchingAfterSave]);

  function handleMove({
    activeId,
    targetStatusId,
    targetIndex,
    previousItemOrderKey,
    nextItemOrderKey,
  }: {
    activeId: string;
    targetStatusId: string | undefined;
    targetIndex: number;
    previousItemOrderKey: string | undefined;
    nextItemOrderKey: string | undefined;
  }) {
    // Prevent a one-frame "snap back" from viewData sync before the store reports hasPending=true
    suppressViewDataSyncRef.current = true;

    const srcColId =
      Object.keys(columnsRef.current).find((key) =>
        columnsRef.current[key].some((item) => item.id === activeId),
      ) ?? "unclassified";

    const dstColId = targetStatusId ?? "unclassified";

    const srcItems = [...(columnsRef.current[srcColId] ?? [])];
    const dstItems =
      srcColId === dstColId
        ? srcItems
        : [...(columnsRef.current[dstColId] ?? [])];

    const activeItem = columnsRef.current[srcColId]?.find((item) => item.id === activeId);
    if (!activeItem) return;

    const isTask = activeItem.__type === "task";

    const newPriority = inferPriorityFromNeighbors({
      activeItem,
      targetIndex,
      dstItems,
    });

    const movedIdx = srcItems.findIndex((item) => item.id === activeId);
    if (movedIdx === -1) return;

    const [movedItem] = srcItems.splice(movedIdx, 1);
    const updatedItem = {
      ...movedItem,
      statusId: targetStatusId,
      priority: newPriority,
    };

    if (srcColId === dstColId) {
      srcItems.splice(targetIndex, 0, updatedItem);
      setColumns((prev) => ({ ...prev, [srcColId]: srcItems }));
    } else {
      dstItems.splice(targetIndex, 0, updatedItem);
      setColumns((prev) => ({
        ...prev,
        [srcColId]: srcItems,
        [dstColId]: dstItems,
      }));
    }

    // Optimistic cache patching
    const queryKey = itemsQueryKey;
    queryClient.setQueryData(
      queryKey,
      (old: TaskViewData | undefined) => {
        if (!old) return old;
        const patchTasks = (list: TaskItemDto[]) =>
          (list ?? []).map((item) =>
            item.id === activeId
              ? { ...item, statusId: targetStatusId, priority: newPriority }
              : item,
          );
        const patchFolders = (list: FolderItemDto[]) =>
          (list ?? []).map((item) =>
            item.id === activeId
              ? { ...item, statusId: targetStatusId, priority: newPriority }
              : item,
          );
        return {
          ...old,
          tasks: patchTasks(old.tasks),
          folders: patchFolders(old.folders),
        };
      },
    );

    // Send to store immediately — store handles the 1500ms debounce
    // statusId: null = explicit unclassify, undefined = don't change (badge-only updates)
    useItemsStore.getState().addUpdate(workspaceId, {
      id: activeId,
      type: isTask ? EntityLayerType.ProjectTask : EntityLayerType.ProjectFolder,
      statusId: targetStatusId ?? null, // null = unclassify → API sends sentinel
      priority: newPriority,
      previousItemOrderKey,
      nextItemOrderKey,
    });
  }

  function handlePriorityChange(itemId: string, priority: Priority) {
    // Optimistically update local columns
    setColumns((prev) => {
      const next = { ...prev };
      for (const key of Object.keys(next)) {
        next[key] = next[key]
          .map((item) => (item.id === itemId ? { ...item, priority } : item))
          .sort(prioritySort);
      }
      return next;
    });

    // Optimistic cache patching
    const queryKey = itemsQueryKey;
    queryClient.setQueryData(queryKey, (old: TaskViewData | undefined) => {
      if (!old) return old;
      const patchTasks = (list: TaskItemDto[]) =>
        (list ?? []).map((item) =>
          item.id === itemId ? { ...item, priority } : item,
        );
      const patchFolders = (list: FolderItemDto[]) =>
        (list ?? []).map((item) =>
          item.id === itemId ? { ...item, priority } : item,
        );
      return {
        ...old,
        tasks: patchTasks(old.tasks),
        folders: patchFolders(old.folders),
      };
    });

    // Find the item to determine its type
    const allItems = Object.values(columnsRef.current).flat();
    const item = allItems.find((i) => i.id === itemId);
    const isTask = item?.__type === "task";

    // Only send priority — statusId is undefined → omitted from JSON → backend keeps existing
    useItemsStore.getState().addUpdate(workspaceId, {
      id: itemId,
      type: isTask ? EntityLayerType.ProjectTask : EntityLayerType.ProjectFolder,
      priority,
    });
  }

  function handleTaskClick(taskId: string) {
    navigate({
      to: "/workspaces/$workspaceId/tasks/$taskId",
      params: { workspaceId, taskId },
    });
  }

  function handleFolderClick(folderId: string) {
    navigate({
      to: "/workspaces/$workspaceId/folders/$folderId",
      params: { workspaceId, folderId },
    });
  }

  const statuses = viewData.statuses ?? [];

  if (viewMode === "board") {
    return (
      <UnifiedBoardView
        columns={columns}
        statuses={statuses}
        onMove={handleMove}
        onTaskClick={handleTaskClick}
        onFolderClick={handleFolderClick}
        onPriorityChange={handlePriorityChange}
      />
    );
  }

  return (
    <UnifiedListView
      columns={columns}
      statuses={statuses}
      onMove={handleMove}
      onTaskClick={handleTaskClick}
      onFolderClick={handleFolderClick}
      onPriorityChange={handlePriorityChange}
    />
  );
}
