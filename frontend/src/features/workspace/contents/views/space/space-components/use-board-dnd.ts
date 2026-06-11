import { useState } from "react";
import { useDispatch } from "react-redux";
import {
  useSensor,
  useSensors,
  PointerSensor,
  TouchSensor,
  type DragStartEvent,
  type DragEndEvent,
} from "@dnd-kit/core";
import { folderSlice, taskSlice } from "@/store/entityStore";
import { fractionalBetween } from "@/features/workspace/contents/hierarchy/utils/fractional-index";
import { getPriorityWeight, WeightToPriority, type Priority } from "@/types/priority";
import type { BoardItem } from "../space-api";
import type { Status } from "@/types/status";

interface UseBoardDndProps {
  spaceId: string;
  boardItems: BoardItem[];
  statuses: Status[];
  columns: Record<string, BoardItem[]>;
  batchUpdate: (args: any) => void;
}

interface PositionResult {
  prevItemOfSamePriority: BoardItem | null;
  nextItemOfSamePriority: BoardItem | null;
  resolvedPriority: Priority;
  tempOrderKey: string;
}

function parseDndId(id: string | number): string {
  const str = String(id);
  if (str.startsWith("task-")) return str.substring(5);
  if (str.startsWith("folder-")) return str.substring(7);
  return str;
}

function resolveTargetColumn(
  overId: string,
  rawOverId: string,
  rawActiveId: string,
  statuses: Status[],
  snapshotCols: Record<string, BoardItem[]>
): string | undefined {
  const isDirectColumnDrop = statuses.some((s) => s.id === overId) || overId === "unclassified";

  if (isDirectColumnDrop) return overId;

  return (
    Object.keys(snapshotCols).find((key) =>
      snapshotCols[key].some((item) => item.id === rawOverId)
    ) ??
    Object.keys(snapshotCols).find((key) =>
      snapshotCols[key].some((item) => item.id === rawActiveId)
    )
  );
}

function resolveCrossColumnPosition(
  activeItem: BoardItem,
  destItems: BoardItem[]
): PositionResult {
  const activeWeight = getPriorityWeight(activeItem);
  let insertIndex = destItems.length;

  for (let i = 0; i < destItems.length; i++) {
    if (getPriorityWeight(destItems[i]) < activeWeight) {
      insertIndex = i;
      break;
    }
  }

  const prevItemOfSamePriority = destItems[insertIndex - 1] ?? null;
  const nextItemOfSamePriority = destItems[insertIndex] ?? null;

  return {
    prevItemOfSamePriority,
    nextItemOfSamePriority,
    resolvedPriority: activeItem.priority as Priority,
    tempOrderKey: fractionalBetween(
      prevItemOfSamePriority?.orderKey ?? null,
      nextItemOfSamePriority?.orderKey ?? null
    ),
  };
}

// Replace the heavy logic inside resolveSameColumnPosition with an optimized version:
function resolveSameColumnPosition(
  rawActiveId: string,
  rawOverId: string,
  destItems: BoardItem[],
  activeItem: BoardItem
): PositionResult | null {
  const activeIndex = destItems.findIndex((item) => item.id === rawActiveId);
  const overIndex = destItems.findIndex((item) => item.id === rawOverId);

  if (activeIndex === -1 || overIndex === -1 || activeIndex === overIndex) return null;

  const reorderedItems = [...destItems];
  const [movingItem] = reorderedItems.splice(activeIndex, 1);
  reorderedItems.splice(overIndex, 0, movingItem);

  const finalIndex = overIndex; // The spliced slot guarantees its destination index

  const prevNeighbor = reorderedItems[finalIndex - 1] ?? null;
  const nextNeighbor = reorderedItems[finalIndex + 1] ?? null;

  let resolvedPriority = activeItem.priority as Priority;

  if (prevNeighbor) {
    resolvedPriority = WeightToPriority[getPriorityWeight(prevNeighbor)];
  } else if (nextNeighbor) {
    resolvedPriority = WeightToPriority[getPriorityWeight(nextNeighbor)];
  }

  const resolvedWeight = getPriorityWeight({ priority: resolvedPriority });

  // Streamlined neighborhood scans
  let prevItemOfSamePriority: BoardItem | null = null;
  let nextItemOfSamePriority: BoardItem | null = null;

  for (let i = finalIndex - 1; i >= 0; i--) {
    if (getPriorityWeight(reorderedItems[i]) === resolvedWeight) {
      prevItemOfSamePriority = reorderedItems[i];
      break;
    }
  }

  for (let i = finalIndex + 1; i < reorderedItems.length; i++) {
    if (getPriorityWeight(reorderedItems[i]) === resolvedWeight) {
      nextItemOfSamePriority = reorderedItems[i];
      break;
    }
  }

  return {
    prevItemOfSamePriority,
    nextItemOfSamePriority,
    resolvedPriority,
    tempOrderKey: fractionalBetween(
      prevItemOfSamePriority?.orderKey ?? null,
      nextItemOfSamePriority?.orderKey ?? null
    ),
  };
}

export function useBoardDnd({
  spaceId,
  boardItems,
  statuses,
  columns,
  batchUpdate,
}: UseBoardDndProps) {
  const dispatch = useDispatch();

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 250, tolerance: 5 } })
  );

  const [draggedItem, setDraggedItem] = useState<BoardItem | null>(null);

  function handleDragStart(event: DragStartEvent) {
    const rawActiveId = parseDndId(event.active.id);
    const item = boardItems.find((i) => i.id === rawActiveId);
    if (item) {
      setDraggedItem(item);
    }
  }

  function handleDragEnd(event: DragEndEvent) {
  const { active, over } = event;
  const snapshotCols = columns; 

  setDraggedItem(null);

  if (!over) return;

  const rawActiveId = parseDndId(active.id);
  const overId = over.id as string;
  const rawOverId = parseDndId(over.id);

  const toColId = resolveTargetColumn(overId, rawOverId, rawActiveId, statuses, snapshotCols);
  if (!toColId) return;

  const activeItem = boardItems.find((i) => i.id === rawActiveId);
  if (!activeItem) return;

  const fromColId = Object.keys(snapshotCols).find((key) =>
    snapshotCols[key].some((item) => item.id === rawActiveId)
  );

  const destItems = snapshotCols[toColId] ?? [];
  const resolvedStatusId = toColId === "unclassified" ? null : toColId;

  const isSameColumn = fromColId === toColId;

  const position = isSameColumn
    ? resolveSameColumnPosition(rawActiveId, rawOverId, destItems, activeItem)
    : resolveCrossColumnPosition(activeItem, destItems);

  if (!position) return;

  const { prevItemOfSamePriority, nextItemOfSamePriority, resolvedPriority, tempOrderKey } = position;

  const updates = {
    id: rawActiveId,
    statusId: resolvedStatusId ?? undefined,
    priority: resolvedPriority,
    orderKey: tempOrderKey,
  };

  // FIX: Wrap the heavy state tracking modifications in a microtask 
  // so the browser can close the pointer drop handler instantly.
  queueMicrotask(() => {
    batchUpdate({
      spaceId,
      updates: [
        {
          id: rawActiveId,
          type: activeItem.__type === "folder" ? "ProjectFolder" : "ProjectTask",
          statusId: resolvedStatusId,
          priority: resolvedPriority,
          orderKey: tempOrderKey,
          previousItemOrderKey: prevItemOfSamePriority?.orderKey ?? null,
          nextItemOrderKey: nextItemOfSamePriority?.orderKey ?? null,
        },
      ],
    });
  });
}
  return { sensors, draggedItem, handleDragStart, handleDragEnd };
}