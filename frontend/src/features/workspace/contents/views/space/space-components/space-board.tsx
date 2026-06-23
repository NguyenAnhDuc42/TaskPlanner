import { useRef, useMemo, useCallback, useState } from "react";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { createPortal } from "react-dom";
import {
  DndContext,
  DragOverlay,
  closestCorners
} from "@dnd-kit/core";
import { Priority, prioritySort } from "@/types/priority";
import {
  useGetSpaceItemsQuery,
  useSpaceBoardItems,
  useSpaceStatuses,
  useBatchUpdateSpaceItemsMutation,
  type BoardItem,
} from "../space-api";
import { BoardItemCard } from "./sortable-board-item";
import { useBoardDnd } from "./use-board-dnd";
import { BoardColumn } from "./board-column";
import { useSmartWheelScroll } from "@/features/workspace/contents/views/space/utils/use-smart-wheel-scroll";
import { useEdgeScroll } from "@/features/workspace/contents/views/space/utils/use-edge-scroll";
import { useDebouncedSpaceBatch } from "@/features/workspace/contents/views/space/utils/use-debounced-space-batch";
import { useUpdateTaskMutation } from "../../task/task-api";
import { SpaceFilterBar } from "./space-filter-bar";
import { EntityLayerType } from "@/types/entity-layer-type";


interface SpaceBoardProps {
  spaceId: string;
  onWorkflowOpen?: () => void;
}

export function SpaceBoard({ spaceId, onWorkflowOpen }: Readonly<SpaceBoardProps>) {
  const navigate = useNavigate({ from: "/workspaces/$workspaceId/spaces/$spaceId" });
  const search = useSearch({ strict: false }) as { contextPanel?: { type: string; id: string } };
  const selectedItemId = search.contextPanel?.id;

  const containerRef = useRef<HTMLDivElement | null>(null);

  const { isLoading } = useGetSpaceItemsQuery(spaceId);
  const boardItems = useSpaceBoardItems(spaceId);
  const statuses = useSpaceStatuses(spaceId);

  const [hiddenStatusIds, setHiddenStatusIds] = useState<string[]>([]);

  const [batchUpdateMutation] = useBatchUpdateSpaceItemsMutation();
  const enqueue = useDebouncedSpaceBatch(batchUpdateMutation, spaceId);

  const columns = useMemo(() => {
    const nextCols: Record<string, BoardItem[]> = {};

    statuses.forEach((s) => { nextCols[s.id] = []; });
    nextCols["unclassified"] = [];

    boardItems.forEach((item) => {
      const colId = item.statusId && nextCols[item.statusId] ? item.statusId : "unclassified";
      nextCols[colId].push(item);
    });

    Object.keys(nextCols).forEach((colId) => {
      nextCols[colId].sort(prioritySort);
    });

    return nextCols;
  }, [boardItems, statuses]);

  const { sensors, draggedItem, handleDragStart, handleDragEnd } = useBoardDnd({
    boardItems,
    statuses,
    columns,
    enqueue,
  });

  const isDragging = draggedItem !== null;
  useSmartWheelScroll(containerRef, isDragging);
  useEdgeScroll(containerRef, isDragging);

  // 2. Click handles are now completely static during a drag execution
  const handleTaskClick = useCallback((id: string) => {
    navigate({
      search: (prev) => {
        const searchParams = prev as Record<string, unknown>;
        return {
          ...searchParams,
          contextPanel: { type: "task", id }
        };
      }
    });
  }, [navigate]);

  const [updateTask] = useUpdateTaskMutation();

  const handleDateChange = useCallback((itemId: string, patches: { startDate?: string; dueDate?: string; clearStartDate?: boolean; clearDueDate?: boolean }) => {
    updateTask({ taskId: itemId, patches });
  }, [updateTask]);

  const handlePriorityChange = useCallback((itemId: string, priority: Priority) => {
    enqueue({ id: itemId, type: EntityLayerType.ProjectTask, priority });
  }, [enqueue]);

  const columnsToRender = useMemo(() => {
    const cols = statuses.map((s) => ({
      id: s.id,
      name: s.name,
      color: s.color,
      category: s.category,
      items: columns[s.id] || [],
    }));

    if (columns["unclassified"] && columns["unclassified"].length > 0) {
      cols.push({
        id: "unclassified",
        name: "Unclassified",
        color: "#6b7280",
        category: "NotStarted",
        items: columns["unclassified"],
      });
    }

    return cols.filter((col) => !hiddenStatusIds.includes(col.id));
  }, [statuses, columns, hiddenStatusIds]);

  if (isLoading && boardItems.length === 0) {
    return (
      <div className="flex-1 flex items-center justify-center text-xs text-muted-foreground">
        Loading board items...
      </div>
    );
  }

  return (
    <>
      <SpaceFilterBar
        statuses={statuses}
        hiddenStatusIds={hiddenStatusIds}
        setHiddenStatusIds={setHiddenStatusIds}
        onWorkflowOpen={onWorkflowOpen}
      />

      <DndContext
        sensors={sensors}
        collisionDetection={closestCorners}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
      >
        <div
          ref={containerRef}
          className="flex-1 flex gap-2 px-2 overflow-x-auto overflow-y-hidden select-none [&::-webkit-scrollbar]:h-1.5 [&::-webkit-scrollbar-thumb]:bg-white/5 [&::-webkit-scrollbar-thumb]:rounded-full hover:[&::-webkit-scrollbar-thumb]:bg-white/[0.15] [&::-webkit-scrollbar-track]:bg-transparent"
        >
          {columnsToRender.map((col) => (
            <BoardColumn
              key={col.id}
              statusId={col.id}
              name={col.name}
              color={col.color}
              category={col.category}
              items={col.items}
              spaceId={spaceId}
              selectedItemId={selectedItemId}
              onTaskClick={handleTaskClick}
              onPriorityChange={handlePriorityChange}
              onDateChange={handleDateChange}
            />
          ))}
        </div>

        {createPortal(
          <DragOverlay dropAnimation={null}>
            {draggedItem ? (
              // Add layout-stable inline styles to prevent container shifting on mount
              <div 
                className="rotate-3 scale-105 opacity-90 pointer-events-none w-67 contain-layout"
                style={{ willChange: "transform" }}
              >
                <BoardItemCard item={draggedItem} isDragging={false} />
              </div>
            ) : null}
          </DragOverlay>,
          document.body
        )}
      </DndContext>
    </>
  );
}
