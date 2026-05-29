import { useState, useRef } from "react";
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
import { getPriorityWeight, type Priority } from "@/types/priority";
import type { BoardItem } from "../space-api";
import type { Status } from "@/types/status";

interface UseBoardDndProps {
  spaceId: string;
  boardItems: BoardItem[];
  statuses: Status[];
  columns: Record<string, BoardItem[]>;
  batchUpdate: (args: any) => void;
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
  const [localColumns, setLocalColumns] = useState<Record<string, BoardItem[]> | null>(null);
  const lastDragOverRef = useRef<{ activeId: string; overId: string } | null>(null);

  function handleDragStart(event: DragStartEvent) {
    const { active } = event;
    const item = boardItems.find((i) => i.id === active.id);
    if (item) {
      setDraggedItem(item);
      setLocalColumns(columns);
      lastDragOverRef.current = null;
    }
  }

  function handleDragOver(event: any) {
    const { active, over } = event;
    if (!over || !localColumns) return;

    const activeId = active.id as string;
    const overId = over.id as string;

    // INSTANT DEBOUNCE: Skip processing if we are dragging over the same active hover target
    if (
      lastDragOverRef.current &&
      lastDragOverRef.current.activeId === activeId &&
      lastDragOverRef.current.overId === overId
    ) {
      return;
    }

    lastDragOverRef.current = { activeId, overId };

    const fromColId = Object.keys(localColumns).find((key) =>
      localColumns[key].some((item) => item.id === activeId)
    );
    if (!fromColId) return;

    const toColId = statuses.some((s) => s.id === overId) || overId === "unclassified"
      ? overId
      : Object.keys(localColumns).find((key) =>
          localColumns[key].some((item) => item.id === overId)
        );

    if (!toColId) return;

    // Only handle cross-column moves here; same-column final position is resolved in handleDragEnd
    if (fromColId !== toColId) {
      setLocalColumns((prev) => {
        if (!prev) return null;
        const sourceItems = [...(prev[fromColId] || [])];
        const destItems = [...(prev[toColId] || [])];

        const activeIndex = sourceItems.findIndex((item) => item.id === activeId);
        if (activeIndex === -1) return prev;

        const [movingItem] = sourceItems.splice(activeIndex, 1);
        
        let targetIndex = destItems.length;
        if (toColId !== overId) {
          const overIndex = destItems.findIndex((item) => item.id === overId);
          if (overIndex !== -1) {
            targetIndex = overIndex;
          }
        }

        destItems.splice(targetIndex, 0, {
          ...movingItem,
          statusId: toColId === "unclassified" ? undefined : toColId,
        });

        return {
          ...prev,
          [fromColId]: sourceItems,
          [toColId]: destItems,
        };
      });
    }
  }

  function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    
    const snapshotCols = localColumns || columns;
    setDraggedItem(null);
    setLocalColumns(null);
    lastDragOverRef.current = null;

    if (!over || !snapshotCols) return;

    const activeId = active.id as string;
    const overId = over.id as string;

    // Find which column the active item is currently in (after cross-col moves via handleDragOver)
    const fromColId = Object.keys(snapshotCols).find((key) =>
      snapshotCols[key].some((item) => item.id === activeId)
    );
    if (!fromColId) return;

    // Determine the target column from the over target
    const isOverStatusHeader = statuses.some((s) => s.id === overId) || overId === "unclassified";
    const overColId = isOverStatusHeader
      ? overId
      : Object.keys(snapshotCols).find((key) =>
          snapshotCols[key].some((item) => item.id === overId)
        ) ?? fromColId;

    const toColId = overColId;

    // Build the final destination items array
    let destItems: BoardItem[];

    if (fromColId === toColId) {
      // SAME-COLUMN REORDER: dnd-kit handled visuals via GPU translate.
      // We must apply the position swap here to get the correct final order.
      const colItems = [...(snapshotCols[fromColId] || [])];
      const activeIndex = colItems.findIndex((i) => i.id === activeId);
      const overIndex = colItems.findIndex((i) => i.id === overId);

      if (activeIndex === -1) return;

      if (overIndex !== -1 && activeIndex !== overIndex) {
        // Remove from original position, insert at target position
        const [moved] = colItems.splice(activeIndex, 1);
        colItems.splice(overIndex, 0, moved);
      }

      destItems = colItems;
    } else {
      // CROSS-COLUMN: handleDragOver already moved the item into the new column in localColumns
      destItems = snapshotCols[toColId] || [];
    }

    const activeIndex = destItems.findIndex((item) => item.id === activeId);
    if (activeIndex === -1) return;

    const activeItem = boardItems.find((i) => i.id === activeId);
    if (!activeItem) return;

    const resolvedStatusId = toColId === "unclassified" ? null : toColId;

    const prevItem = destItems[activeIndex - 1];
    const nextItem = destItems[activeIndex + 1];

    let resolvedPriority = activeItem.priority as Priority;
    const activeWeight = getPriorityWeight(activeItem);
    const prevWeight = prevItem ? getPriorityWeight(prevItem) : null;
    const nextWeight = nextItem ? getPriorityWeight(nextItem) : null;

    if (prevWeight === null && nextWeight !== null) {
      if (activeWeight !== nextWeight) {
        resolvedPriority = (nextItem as any).priority as Priority;
      }
    } else if (prevWeight !== null && nextWeight === null) {
      if (activeWeight !== prevWeight) {
        resolvedPriority = (prevItem as any).priority as Priority;
      }
    } else if (prevWeight !== null && nextWeight !== null) {
      if (activeWeight > prevWeight) {
        resolvedPriority = (prevItem as any).priority as Priority;
      } else if (activeWeight < nextWeight) {
        resolvedPriority = (nextItem as any).priority as Priority;
      }
    }

    let tempOrderKey = activeItem.orderKey;
    const isPrevSamePriority = prevItem && (prevItem as any).priority === resolvedPriority;
    const isNextSamePriority = nextItem && (nextItem as any).priority === resolvedPriority;

    if (isPrevSamePriority && isNextSamePriority) {
      tempOrderKey = fractionalBetween(prevItem?.orderKey, nextItem?.orderKey);
    } else if (isPrevSamePriority) {
      tempOrderKey = fractionalBetween(prevItem?.orderKey, null);
    } else if (isNextSamePriority) {
      tempOrderKey = fractionalBetween(null, nextItem?.orderKey);
    } else {
      tempOrderKey = activeItem.orderKey || fractionalBetween(null, null);
    }

    const updates = {
      id: activeId,
      statusId: resolvedStatusId ?? undefined,
      priority: resolvedPriority,
      orderKey: tempOrderKey,
    };

    if (activeItem.__type === "folder") {
      dispatch(folderSlice.actions.upsert(updates));
    } else {
      dispatch(taskSlice.actions.upsert(updates));
    }

    batchUpdate({
      spaceId,
      updates: [
        {
          id: activeId,
          type: activeItem.__type === "folder" ? "ProjectFolder" : "ProjectTask",
          statusId: resolvedStatusId,
          priority: resolvedPriority,
          orderKey: tempOrderKey,
          previousItemOrderKey: prevItem?.orderKey || null,
          nextItemOrderKey: nextItem?.orderKey || null,
        },
      ],
    });
  }

  return { sensors, draggedItem, localColumns, handleDragStart, handleDragOver, handleDragEnd };
}
