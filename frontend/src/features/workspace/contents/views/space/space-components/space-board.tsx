import { useState, useRef, useMemo, useEffect } from "react";
import { useParams, useNavigate } from "@tanstack/react-router";
import { useSelector, useDispatch } from "react-redux";
import {
  DndContext,
  useSensor,
  useSensors,
  PointerSensor,
  TouchSensor,
  type DragStartEvent,
  type DragEndEvent,
  DragOverlay,
} from "@dnd-kit/core";
import { Priority } from "@/types/priority";
import {
  useGetSpaceItemsQuery,
  useSpaceBoardItems,
  useBatchUpdateSpaceItemsMutation,
  type BoardItem,
} from "../space-api";
import { folderSlice, taskSlice } from "@/store/entityStore";
import { TaskItem } from "@/features/workspace/contents/layer-detail/components/items/task-item";
import { FolderItem } from "@/features/workspace/contents/layer-detail/components/items/folder-item";
import { BoardColumn } from "./board-column";
import { StatusBadge } from "@/components/status-badge";
import { GitMerge } from "lucide-react";
import { useSmartWheelScroll } from "@/features/workspace/contents/layer-detail/components/board/use-smart-wheel-scroll";
import { useEdgeScroll } from "@/features/workspace/contents/layer-detail/components/board/use-edge-scroll";

interface SpaceBoardProps {
  spaceId: string;
  onWorkflowOpen?: () => void;
}

export function SpaceBoard({ spaceId, onWorkflowOpen }: SpaceBoardProps) {
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const { workspaceId } = useParams({ strict: false }) as { workspaceId: string };
  const containerRef = useRef<HTMLDivElement | null>(null);

  // Load items from server into Redux (automatically upserts)
  const { data: spaceItems, isLoading } = useGetSpaceItemsQuery(spaceId);

  // Read statuses directly from cache
  const statuses = spaceItems?.statuses || [];

  // Read flat board items from Redux
  const boardItems = useSpaceBoardItems(spaceId);

  // Mutator for updating space items
  const [batchUpdate] = useBatchUpdateSpaceItemsMutation();

  // Combine and group board items by status ID
  const columns = useMemo(() => {
    const cols: Record<string, BoardItem[]> = {};
    statuses.forEach((s) => {
      cols[s.id] = [];
    });
    cols["unclassified"] = [];

    boardItems.forEach((item) => {
      const colId = item.statusId && cols[item.statusId] ? item.statusId : "unclassified";
      cols[colId].push(item);
    });

    // Sort by orderKey ascending
    Object.keys(cols).forEach((colId) => {
      cols[colId].sort((a, b) => (a.orderKey || "").localeCompare(b.orderKey || ""));
    });

    return cols;
  }, [boardItems, statuses]);

  // Drag states
  const [draggedItem, setDraggedItem] = useState<BoardItem | null>(null);
  const [localColumns, setLocalColumns] = useState<Record<string, BoardItem[]>>({});

  // Sync local columns when Redux updates (only when not dragging)
  useEffect(() => {
    if (!draggedItem) {
      setLocalColumns(columns);
    }
  }, [columns, draggedItem]);

  // Enable custom board-wide scrolling gestures
  const isDragging = draggedItem !== null;
  useSmartWheelScroll(containerRef, isDragging);
  useEdgeScroll(containerRef, isDragging);

  // Drag Sensors
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 250, tolerance: 5 } })
  );

  function handleDragStart(event: DragStartEvent) {
    const { active } = event;
    const item = boardItems.find((i) => i.id === active.id);
    if (item) {
      setDraggedItem(item);
    }
  }

  function handleDragOver(event: any) {
    const { active, over } = event;
    if (!over) return;

    const activeId = active.id as string;
    const overId = over.id as string;

    const fromColId = Object.keys(localColumns).find((key) =>
      localColumns[key].some((item) => item.id === activeId)
    );
    const toColId = localColumns[overId]
      ? overId
      : Object.keys(localColumns).find((key) =>
          localColumns[key].some((item) => item.id === overId)
        );

    if (!fromColId || !toColId || fromColId === toColId) return;

    setLocalColumns((prev) => {
      const sourceCol = prev[fromColId] || [];
      const destCol = prev[toColId] || [];
      const itemToMove = sourceCol.find((i) => i.id === activeId);
      if (!itemToMove) return prev;

      const nextSource = sourceCol.filter((i) => i.id !== activeId);
      const overIndex = destCol.findIndex((i) => i.id === overId);
      const insertIndex = overIndex === -1 ? destCol.length : overIndex;

      const nextDest = [...destCol];
      nextDest.splice(insertIndex, 0, {
        ...itemToMove,
        statusId: toColId === "unclassified" ? null : toColId,
      } as any);

      return {
        ...prev,
        [fromColId]: nextSource,
        [toColId]: nextDest,
      };
    });
  }

  function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    setDraggedItem(null);

    if (!over) {
      setLocalColumns(columns);
      return;
    }

    const activeId = active.id as string;

    const targetColId = Object.keys(localColumns).find((key) =>
      localColumns[key].some((item) => item.id === activeId)
    );
    if (!targetColId) {
      setLocalColumns(columns);
      return;
    }

    const targetItems = localColumns[targetColId] || [];
    const targetIndex = targetItems.findIndex((item) => item.id === activeId);

    const stripped = targetItems.filter((i) => i.id !== activeId);
    const clampedIndex = Math.max(0, Math.min(targetIndex, stripped.length));
    const prevItem = stripped[clampedIndex - 1];
    const nextItem = stripped[clampedIndex];

    const resolvedStatusId = targetColId === "unclassified" ? null : targetColId;

    const activeDetails = boardItems.find((i) => i.id === activeId);
    if (activeDetails) {
      const updates = {
        id: activeId,
        statusId: resolvedStatusId ?? undefined,
      };
      if (activeDetails.__type === "folder") {
        dispatch(folderSlice.actions.upsert(updates));
      } else {
        dispatch(taskSlice.actions.upsert(updates));
      }

      batchUpdate({
        spaceId,
        updates: [
          {
            id: activeId,
            type: activeDetails.__type === "folder" ? "ProjectFolder" : "ProjectTask",
            statusId: resolvedStatusId,
            previousItemOrderKey: prevItem?.orderKey || null,
            nextItemOrderKey: nextItem?.orderKey || null,
          },
        ],
      });
    }
  }

  function handlePriorityChange(itemId: string, type: "task" | "folder", priority: Priority) {
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
  }

  const columnsToRender = useMemo(() => {
    const cols = statuses.map((s) => ({
      id: s.id,
      name: s.name,
      color: s.color,
      items: localColumns[s.id] || [],
    }));

    if (localColumns["unclassified"] && localColumns["unclassified"].length > 0) {
      cols.push({
        id: "unclassified",
        name: "Unclassified",
        color: "#6b7280",
        items: localColumns["unclassified"],
      });
    }

    return cols;
  }, [statuses, localColumns]);

  const onTaskClick = (taskId: string) => {
    navigate({ to: `/workspaces/${workspaceId}/tasks/${taskId}` });
  };

  const onFolderClick = (folderId: string) => {
    navigate({ to: `/workspaces/${workspaceId}/folders/${folderId}` });
  };

  if (isLoading && boardItems.length === 0) {
    return (
      <div className="flex-1 flex items-center justify-center text-xs text-muted-foreground">
        Loading board items...
      </div>
    );
  }

  return (
    <DndContext
      sensors={sensors}
      onDragStart={handleDragStart}
      onDragOver={handleDragOver}
      onDragEnd={handleDragEnd}
    >
      {/* Board Top Status Bar & Navigation */}
      <div className="px-6 py-2 border-b border-border/10 flex items-center shrink-0 select-none gap-1 bg-background/20 backdrop-blur-sm">
        {onWorkflowOpen && (
          <button
            className="flex items-center h-6 gap-1 px-2 rounded-md bg-muted/40 text-[10px] text-muted-foreground font-semibold hover:bg-muted/80 hover:text-foreground border border-border/30 transition-all cursor-pointer shrink-0"
            onClick={onWorkflowOpen}
          >
            <GitMerge className="h-3.5 w-3.5 opacity-70" />
            <span>Workflow Settings</span>
          </button>
        )}

        <div className="h-4 w-px bg-border/40 shrink-0" />

        <div className="flex items-center gap-1 overflow-x-auto [&::-webkit-scrollbar]:hidden [-ms-overflow-style:none] [scrollbar-width:none] h-6">
          {statuses.map((status) => (
            <button
              key={status.id}
              onClick={() => {
                const el = document.getElementById(status.id);
                if (el) {
                  el.scrollIntoView({ behavior: "smooth", block: "nearest", inline: "center" });
                }
              }}
              className="cursor-pointer shrink-0"
            >
              <StatusBadge status={status} variant="outline" className="h-6 flex items-center" />
            </button>
          ))}
        </div>
      </div>

      <div
        ref={containerRef}
        className="flex-1 flex gap-4 p-6 overflow-x-auto overflow-y-hidden select-none [&::-webkit-scrollbar]:h-1.5 [&::-webkit-scrollbar-thumb]:bg-white/[0.05] [&::-webkit-scrollbar-thumb]:rounded-full hover:[&::-webkit-scrollbar-thumb]:bg-white/[0.15] [&::-webkit-scrollbar-track]:bg-transparent"
      >
        {columnsToRender.map((col) => (
          <BoardColumn
            key={col.id}
            statusId={col.id}
            name={col.name}
            color={col.color}
            items={col.items}
            onTaskClick={onTaskClick}
            onFolderClick={onFolderClick}
            onPriorityChange={handlePriorityChange}
          />
        ))}
      </div>

      <DragOverlay dropAnimation={null}>
        {draggedItem ? (
          <div className="rotate-3 scale-105 opacity-90 cursor-grabbing pointer-events-none">
            {draggedItem.__type === "task" ? (
              <TaskItem task={draggedItem as any} onClick={() => {}} />
            ) : (
              <FolderItem folder={draggedItem as any} onClick={() => {}} />
            )}
          </div>
        ) : null}
      </DragOverlay>
    </DndContext>
  );
}
