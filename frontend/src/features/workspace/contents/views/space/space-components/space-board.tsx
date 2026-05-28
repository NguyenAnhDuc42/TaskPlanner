import { useState, useRef, useMemo, useEffect } from "react";
import { useParams, useNavigate } from "@tanstack/react-router";
import {  useDispatch } from "react-redux";
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
import { folderSlice, statusSlice, taskSlice } from "@/store/entityStore";
import { BoardItemCard } from "./sortable-board-item";
import { prioritySort, getPriorityWeight } from "@/types/priority";
import { BoardColumn } from "./board-column";
import { StatusBadge } from "@/components/status-badge";
import { GitMerge } from "lucide-react";
import { useSmartWheelScroll } from "@/features/workspace/contents/layer-detail/components/board/use-smart-wheel-scroll";
import { useEdgeScroll } from "@/features/workspace/contents/layer-detail/components/board/use-edge-scroll";
import { fractionalBetween } from "@/features/workspace/contents/hierarchy/utils/fractional-index";

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

  // Track last data state to allow query refetches to reload slices while preventing render loops
  const lastDataRef = useRef<any>(null);

  // Always guarantee Redux entity slices are populated whenever query data is present
  useEffect(() => {
    if (spaceItems && spaceItems !== lastDataRef.current) {
      dispatch(folderSlice.actions.upsertMany(spaceItems.folders));
      dispatch(taskSlice.actions.upsertMany(spaceItems.tasks));
      dispatch(statusSlice.actions.upsertMany(spaceItems.statuses));
      lastDataRef.current = spaceItems;
    }
  }, [spaceItems, dispatch]);

  // Read statuses directly from cache with stable reference memoization
  const statuses = useMemo(() => spaceItems?.statuses || [], [spaceItems?.statuses]);

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

    // Sort by prioritySort (which handles priority weight first, then orderKey)
    Object.keys(cols).forEach((colId) => {
      cols[colId].sort(prioritySort);
    });

    return cols;
  }, [boardItems, statuses]);

  // Drag states
  const [draggedItem, setDraggedItem] = useState<BoardItem | null>(null);

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

  function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    setDraggedItem(null);

    if (!over) return;

    const activeId = active.id as string;
    const overId = over.id as string;

    // 1. Find the source column
    const fromColId = Object.keys(columns).find((key) =>
      columns[key].some((item) => item.id === activeId)
    );
    if (!fromColId) return;

    // 2. Find the destination column (either dropped on empty column, or card inside column)
    const toColId = statuses.some((s) => s.id === overId) || overId === "unclassified"
      ? overId
      : Object.keys(columns).find((key) =>
          columns[key].some((item) => item.id === overId)
        );

    if (!toColId) return;

    // 3. Resolve insertion index in destination column
    const destItems = [...(columns[toColId] || [])];
    const activeItem = boardItems.find((i) => i.id === activeId);
    if (!activeItem) return;

    // Filter out the active item if it was in the same column
    const strippedDest = destItems.filter((item) => item.id !== activeId);

    let targetIndex = strippedDest.length; // Default to end of column
    if (toColId !== overId) {
      const overIndex = strippedDest.findIndex((item) => item.id === overId);
      if (overIndex !== -1) {
        const isSameColumn = fromColId === toColId;
        const originalIndex = columns[fromColId].findIndex((item) => item.id === activeId);
        const originalOverIndex = columns[toColId].findIndex((item) => item.id === overId);
        
        // If same column and dragging downwards, insert AFTER the over item.
        // Otherwise (dragging upwards, or cross-column), insert BEFORE.
        if (isSameColumn && originalIndex !== -1 && originalOverIndex !== -1 && originalIndex < originalOverIndex) {
          targetIndex = overIndex + 1;
        } else {
          targetIndex = overIndex;
        }
      }
    }

    // 4. Neighbors surrounding the dropped position
    const prevItem = strippedDest[targetIndex - 1];
    const nextItem = strippedDest[targetIndex];
    const resolvedStatusId = toColId === "unclassified" ? null : toColId;

    // 5. Dynamically calculate the correct priority weight so the item fits the drop zone perfectly
    let resolvedPriority = activeItem.priority as Priority;
    const activeWeight = getPriorityWeight(activeItem);
    const prevWeight = prevItem ? getPriorityWeight(prevItem) : null;
    const nextWeight = nextItem ? getPriorityWeight(nextItem) : null;

    if (prevWeight === null && nextWeight !== null) {
      // Dropped at the very top of the column
      if (activeWeight !== nextWeight) {
        resolvedPriority = (nextItem as any).priority as Priority;
      }
    } else if (prevWeight !== null && nextWeight === null) {
      // Dropped at the very bottom of the column
      if (activeWeight !== prevWeight) {
        resolvedPriority = (prevItem as any).priority as Priority;
      }
    } else if (prevWeight !== null && nextWeight !== null) {
      // Dropped in the middle
      if (activeWeight > prevWeight) {
        // Dragged from top down
        resolvedPriority = (prevItem as any).priority as Priority;
      } else if (activeWeight < nextWeight) {
        // Dragged from bottom up
        resolvedPriority = (nextItem as any).priority as Priority;
      }
    }

    // Calculate the new order key on the front-end using the same fractional indexing algorithm
    // We can only use fractionalBetween if both neighbors share the same resolved priority.
    // If they have different priorities, we are at a boundary, and fractionalBetween(prev, next) would fail because they aren't guaranteed to be sequentially ordered across groups.
    let tempOrderKey = activeItem.orderKey;
    
    const isPrevSamePriority = prevItem && (prevItem as any).priority === resolvedPriority;
    const isNextSamePriority = nextItem && (nextItem as any).priority === resolvedPriority;

    if (isPrevSamePriority && isNextSamePriority) {
      // Dropped strictly inside a matching priority group
      tempOrderKey = fractionalBetween(prevItem?.orderKey, nextItem?.orderKey);
    } else if (isPrevSamePriority) {
      // Dropped at the very bottom of a matching priority group
      tempOrderKey = fractionalBetween(prevItem?.orderKey, null);
    } else if (isNextSamePriority) {
      // Dropped at the very top of a matching priority group
      tempOrderKey = fractionalBetween(null, nextItem?.orderKey);
    } else {
      // Dropped where neither neighbor matches (forming a new standalone priority group in this column)
      // Keep its existing orderKey, or generate a fresh one since it's the only item in its group
      tempOrderKey = activeItem.orderKey || fractionalBetween(null, null);
    }

    // 6. Update local Redux store
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

    // 7. Sync with database
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
      items: columns[s.id] || [],
    }));

    if (columns["unclassified"] && columns["unclassified"].length > 0) {
      cols.push({
        id: "unclassified",
        name: "Unclassified",
        color: "#6b7280",
        items: columns["unclassified"],
      });
    }

    return cols;
  }, [statuses, columns]);

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
      onDragEnd={handleDragEnd}
    >
      {/* Board Top Status Bar & Navigation */}
      <div className="px-3 py-2 border-b border-border/10 flex items-center shrink-0 select-none gap-1 bg-background/20 backdrop-blur-sm">
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
        className="flex-1 flex gap-3 px-3 overflow-x-auto overflow-y-hidden select-none [&::-webkit-scrollbar]:h-1.5 [&::-webkit-scrollbar-thumb]:bg-white/[0.05] [&::-webkit-scrollbar-thumb]:rounded-full hover:[&::-webkit-scrollbar-thumb]:bg-white/[0.15] [&::-webkit-scrollbar-track]:bg-transparent"
      >
        {columnsToRender.map((col) => (
          <BoardColumn
            key={col.id}
            statusId={col.id}
            name={col.name}
            color={col.color}
            items={col.items}
            spaceId={spaceId}
            onTaskClick={onTaskClick}
            onFolderClick={onFolderClick}
            onPriorityChange={handlePriorityChange}
          />
        ))}
      </div>

      <DragOverlay dropAnimation={null}>
        {draggedItem ? (
          <div className="rotate-3 scale-105 opacity-90 cursor-grabbing pointer-events-none w-[268px]">
            <BoardItemCard item={draggedItem} />
          </div>
        ) : null}
      </DragOverlay>
    </DndContext>
  );
}
