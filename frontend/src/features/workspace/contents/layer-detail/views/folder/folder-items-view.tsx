import { useState, useEffect, useRef } from "react";
import { useNavigate, useParams } from "@tanstack/react-router";
import { useQueryClient } from "@tanstack/react-query";
import { workspaceKeys } from "@/features/main/query-keys";
import type { TaskViewData } from "../../layer-detail-types";
import { buildColumns } from "./folder-dnd-helpers";
import {
  getPriorityWeight,
  WeightToPriority,
  prioritySort,
} from "@/types/priority";
import { useMoveTaskToStatus } from "../task/task-api";
import { UnifiedBoardView } from "../unified-board-view";
import { UnifiedListView } from "../unified-list-view";

interface FolderItemsViewProps {
  viewData: TaskViewData;
  folderId: string;
  viewMode: "board" | "list";
}

export function FolderItemsView({
  viewData,
  folderId,
  viewMode,
}: FolderItemsViewProps) {
  const navigate = useNavigate();
  const { workspaceId } = useParams({ strict: false }) as any;
  const queryClient = useQueryClient();

  const [columns, setColumns] = useState<Record<string, any[]>>(() =>
    buildColumns(viewData),
  );

  const columnsRef = useRef(columns);
  columnsRef.current = columns;

  const viewDataRef = useRef<TaskViewData | null>(null);

  const { mutate: moveTaskToStatus, isPending: isMovingTask } =
    useMoveTaskToStatus();

  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const isDebouncingRef = useRef(false);
  const pendingMutationRef = useRef<(() => void) | null>(null);

  useEffect(() => {
    if (isMovingTask || isDebouncingRef.current) return;

    viewDataRef.current = viewData;
    setColumns(buildColumns(viewData));
  }, [viewData, isMovingTask]);

  useEffect(() => {
    return () => {
      if (timeoutRef.current) clearTimeout(timeoutRef.current);
      pendingMutationRef.current?.();
    };
  }, []);

  function handleMove({
    activeId,
    targetStatusId,
    targetIndex,
  }: {
    activeId: string;
    targetStatusId: string | undefined;
    targetIndex: number;
    previousItemOrderKey: string | undefined;
    nextItemOrderKey: string | undefined;
  }) {
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

    // Calculate boundary priorities & orderKeys
    const stripped = dstItems.filter((item) => item.id !== activeId);
    const clampedIndex = Math.max(0, Math.min(targetIndex, stripped.length));
    const prev = stripped[clampedIndex - 1];
    const next = stripped[clampedIndex];

    let newWeight: number;
    if (!prev) {
      newWeight = next ? getPriorityWeight(next) : 1;
    } else if (!next) {
      newWeight = getPriorityWeight(prev);
    } else {
      const prevWeight = getPriorityWeight(prev);
      const nextWeight = getPriorityWeight(next);
      if (prevWeight === nextWeight) {
        newWeight = prevWeight;
      } else {
        newWeight = Math.floor((prevWeight + nextWeight) / 2);
      }
    }

    const newPriority = WeightToPriority[newWeight] ?? "Normal";

    // Isolate identical priority subgroup in destination to determine mid-orderKey
    const samePriorityItems = stripped.filter(
      (item) => getPriorityWeight(item) === newWeight
    );

    const k = stripped.slice(0, clampedIndex).filter(
      (item) => getPriorityWeight(item) === newWeight
    ).length;

    const prevSame = samePriorityItems[k - 1];
    const nextSame = samePriorityItems[k];

    const previousItemOrderKey = prevSame?.orderKey;
    const nextItemOrderKey = nextSame?.orderKey;

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
    const queryKey = [...workspaceKeys.all, "folder", folderId, "items"];
    queryClient.setQueryData(
      queryKey,
      (old: any) => {
        if (!old) return old;
        const patch = (list: any[]) =>
          (list ?? []).map((item: any) =>
            item.id === activeId
              ? { ...item, statusId: targetStatusId, priority: newPriority }
              : item,
          );
        return {
          ...old,
          tasks: patch(old.tasks),
          folders: patch(old.folders),
        };
      },
    );

    isDebouncingRef.current = true;
    if (timeoutRef.current) clearTimeout(timeoutRef.current);

    const persist = () => {
      isDebouncingRef.current = false;
      pendingMutationRef.current = null;
      moveTaskToStatus({
        taskId: activeId,
        targetStatusId,
        previousItemOrderKey,
        nextItemOrderKey,
        newPriority,
      });
    };
    pendingMutationRef.current = persist;
    timeoutRef.current = setTimeout(persist, 1000);
  }

  function handleTaskClick(taskId: string) {
    navigate({
      to: "/workspaces/$workspaceId/tasks/$taskId",
      params: { workspaceId, taskId },
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
        onFolderClick={() => {}}
      />
    );
  }

  return (
    <UnifiedListView
      columns={columns}
      statuses={statuses}
      onMove={handleMove}
      onTaskClick={handleTaskClick}
      onFolderClick={() => {}}
    />
  );
}
