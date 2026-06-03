import { useRef, useMemo, useCallback, useState } from "react";
import { useParams, useNavigate } from "@tanstack/react-router";
import { useDispatch } from "react-redux";
import { createPortal } from "react-dom";
import {
  DndContext,
  DragOverlay,
  pointerWithin
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
import { StatusBadge } from "@/components/status-badge";
import { GitMerge, SlidersHorizontal } from "lucide-react";
import { useSmartWheelScroll } from "@/features/workspace/contents/views/space/utils/use-smart-wheel-scroll";
import { useEdgeScroll } from "@/features/workspace/contents/views/space/utils/use-edge-scroll";

import { folderSlice, taskSlice } from "@/store/entityStore";

interface SpaceBoardProps {
  spaceId: string;
  onWorkflowOpen?: () => void;
}

export function SpaceBoard({ spaceId, onWorkflowOpen }: Readonly<SpaceBoardProps>) {
  const dispatch = useDispatch();
  const navigate = useNavigate();
  
  // 1. CRITICAL FIX: Destructure the string primitive immediately. 
  // Do not depend on the raw params object inside useCallback dependencies.
  const params = useParams({ strict: false }) as Record<string, string>;
  const workspaceId = params?.workspaceId;

  const containerRef = useRef<HTMLDivElement | null>(null);

  const { isLoading } = useGetSpaceItemsQuery(spaceId);
  const boardItems = useSpaceBoardItems(spaceId);
  const statuses = useSpaceStatuses(spaceId);

  const [hiddenStatusIds, setHiddenStatusIds] = useState<string[]>([]);

  const toggleStatusVisibility = useCallback((statusId: string) => {
    setHiddenStatusIds((prev) =>
      prev.includes(statusId)
        ? prev.filter((id) => id !== statusId)
        : [...prev, statusId]
    );
  }, []);
  const stableColumnsRef = useRef<Record<string, BoardItem[]>>({});
  const [batchUpdate] = useBatchUpdateSpaceItemsMutation();

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
  Object.keys(nextCols).forEach((key) => {
    const prev = stableColumnsRef.current[key];
    const curr = nextCols[key];
    if (prev?.length === curr.length && prev.every((v, i) => v.id === curr[i].id && v.orderKey === curr[i].orderKey)) {
      nextCols[key] = prev;
    }
  });

  stableColumnsRef.current = nextCols;
  return nextCols;
}, [boardItems, statuses]);

  const { sensors, draggedItem, handleDragStart, handleDragEnd } = useBoardDnd({
    spaceId,
    boardItems,
    statuses,
    columns,
    batchUpdate,
  });

  const isDragging = draggedItem !== null;
  useSmartWheelScroll(containerRef, isDragging);
  useEdgeScroll(containerRef, isDragging);

  // 2. Click handles are now completely static during a drag execution
  const handleTaskClick = useCallback((id: string) => {
    if (!workspaceId) return;
    navigate({ to: `/workspaces/${workspaceId}/tasks/${id}` });
  }, [workspaceId, navigate]);

  const handleFolderClick = useCallback((id: string) => {
    if (!workspaceId) return;
    navigate({ to: `/workspaces/${workspaceId}/folders/${id}` });
  }, [workspaceId, navigate]);

  const handlePriorityChange = useCallback((itemId: string, type: "task" | "folder", priority: Priority) => {
    const updates = { id: itemId, priority };
    if (type === "folder") {
      dispatch(folderSlice.actions.upsert(updates));
    } else {
      dispatch(taskSlice.actions.upsert(updates));
    }

    batchUpdate({
      spaceId,
      updates: [
        {
          id: itemId,
          type: type === "folder" ? "ProjectFolder" : "ProjectTask",
          priority,
        },
      ],
    });
  }, [spaceId, batchUpdate, dispatch]);

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
      <div className="px-2 py-2 flex items-center shrink-0 select-none gap-1 bg-background/20 backdrop-blur-sm">
        {onWorkflowOpen && (
          <button
            className="flex items-center h-6 gap-1 px-2 rounded-md bg-muted/40 text-[10px] text-muted-foreground font-semibold hover:bg-muted/80 hover:text-foreground border border-border/30 transition-all cursor-pointer shrink-0"
            onClick={onWorkflowOpen}
          >
            <GitMerge className="h-3.5 w-3.5 opacity-70" />
            <span>Workflow</span>
          </button>
        )}

        <div className="flex items-center gap-1 overflow-x-auto [&::-webkit-scrollbar]:hidden [-ms-overflow-style:none] [scrollbar-width:none] h-6">
          {statuses.map((status) => (
            <button
              key={status.id}
              onClick={() => toggleStatusVisibility(status.id)}
              className={`cursor-pointer shrink-0 transition-all duration-200 ${
                hiddenStatusIds.includes(status.id) ? "opacity-25 saturate-[0.1]" : ""
              }`}
            >
              <StatusBadge status={status} variant="outline" className="h-6 flex items-center" />
            </button>
          ))}
        </div>

        <button
          className="ml-auto flex items-center h-6 gap-1 px-2 rounded-md bg-muted/40 text-[10px] text-muted-foreground font-semibold hover:bg-muted/80 hover:text-foreground border border-border/30 transition-all cursor-pointer shrink-0"
          onClick={() => {
            console.log("Filter clicked");
          }}
        >
          <SlidersHorizontal className="h-3 w-3 opacity-70" />
          <span>Filter</span>
        </button>
      </div>

      <DndContext
        sensors={sensors}
        collisionDetection={pointerWithin}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
      >
        <div
          ref={containerRef}
          className="flex-1 flex gap-2 px-2 overflow-x-auto overflow-y-hidden select-none [&::-webkit-scrollbar]:h-1.5 [&::-webkit-scrollbar-thumb]:bg-white/[0.05] [&::-webkit-scrollbar-thumb]:rounded-full hover:[&::-webkit-scrollbar-thumb]:bg-white/[0.15] [&::-webkit-scrollbar-track]:bg-transparent"
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
              onTaskClick={handleTaskClick}
              onFolderClick={handleFolderClick}
              onPriorityChange={handlePriorityChange}
            />
          ))}
        </div>

        {createPortal(
          <DragOverlay dropAnimation={null}>
            {draggedItem ? (
              // Add layout-stable inline styles to prevent container shifting on mount
              <div 
                className="rotate-3 scale-105 opacity-90 pointer-events-none w-[268px] contain-layout"
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