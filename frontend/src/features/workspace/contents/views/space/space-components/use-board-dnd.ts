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

  function handleDragStart(event: DragStartEvent) {
    const { active } = event;
    const item = boardItems.find((i) => i.id === active.id);
    if (item) {
      setDraggedItem(item);
      setLocalColumns(columns);
    }
  }

  function handleDragOver(event: any) {
    const { active, over } = event;
    if (!over || !localColumns) return;

    const activeId = active.id as string;
    const overId = over.id as string;

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
    } else {
      setLocalColumns((prev) => {
        if (!prev) return null;
        const colItems = [...(prev[fromColId] || [])];
        const activeIndex = colItems.findIndex((item) => item.id === activeId);
        const overIndex = colItems.findIndex((item) => item.id === overId);

        if (activeIndex !== -1 && overIndex !== -1 && activeIndex !== overIndex) {
          const [movingItem] = colItems.splice(activeIndex, 1);
          colItems.splice(overIndex, 0, movingItem);
          return {
            ...prev,
            [fromColId]: colItems,
          };
        }
        return prev;
      });
    }
  }

  function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    
    const snapshotCols = localColumns || columns;
    setDraggedItem(null);
    setLocalColumns(null);

    if (!over || !snapshotCols) return;

    const activeId = active.id as string;

    const toColId = Object.keys(snapshotCols).find((key) =>
      snapshotCols[key].some((item) => item.id === activeId)
    );
    if (!toColId) return;

    const destItems = snapshotCols[toColId] || [];
    const activeIndex = destItems.findIndex((item) => item.id === activeId);
    if (activeIndex === -1) return;

    const activeItem = boardItems.find((i) => i.id === activeId);
    if (!activeItem) return;

    const resolvedStatusId = toColId === "unclassified" ? null : toColId;

    const prevItem = destItems[activeIndex - 1];
    const nextItem = destItems[activeIndex + 1];

    let resolvedPriority = activeItem.priority as Priority;
    if (prevItem) {
      resolvedPriority = WeightToPriority[getPriorityWeight(prevItem)];
    } else if (nextItem) {
      resolvedPriority = WeightToPriority[getPriorityWeight(nextItem)];
    }

    const resolvedWeight = getPriorityWeight({ priority: resolvedPriority });

    let prevItemOfSamePriority: BoardItem | null = null;
    for (let i = activeIndex - 1; i >= 0; i--) {
      if (getPriorityWeight(destItems[i]) === resolvedWeight) {
        prevItemOfSamePriority = destItems[i];
        break;
      }
    }

    let nextItemOfSamePriority: BoardItem | null = null;
    for (let i = activeIndex + 1; i < destItems.length; i++) {
      if (getPriorityWeight(destItems[i]) === resolvedWeight) {
        nextItemOfSamePriority = destItems[i];
        break;
      }
    }

    const tempOrderKey = fractionalBetween(
      prevItemOfSamePriority?.orderKey || null,
      nextItemOfSamePriority?.orderKey || null
    );

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
          previousItemOrderKey: prevItemOfSamePriority?.orderKey || null,
          nextItemOrderKey: nextItemOfSamePriority?.orderKey || null,
        },
      ],
    });
  }

  return { sensors, draggedItem, localColumns, handleDragStart, handleDragOver, handleDragEnd };
}
