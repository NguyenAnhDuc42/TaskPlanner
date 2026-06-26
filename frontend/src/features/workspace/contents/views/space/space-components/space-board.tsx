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
  useSpaceItemsFullLoad,
  useSpaceBoardItems,
  useSpaceStatuses,
  useBatchUpdateSpaceItemsMutation,
  type BoardItem,
  type SpaceBoardFilter,
} from "../space-api";
import { useSelector } from "react-redux";
import { folderSelectors } from "@/store/entityStore";
import type { RootState } from "@/store";
import { useDebounce } from "@/hooks/use-debounce";
import { BoardItemCard } from "./sortable-board-item";
import { useBoardDnd } from "./use-board-dnd";
import { BoardColumn } from "./board-column";
import { useSmartWheelScroll } from "@/features/workspace/contents/views/space/utils/use-smart-wheel-scroll";
import { useEdgeScroll } from "@/features/workspace/contents/views/space/utils/use-edge-scroll";
import { useDebouncedSpaceBatch } from "@/features/workspace/contents/views/space/utils/use-debounced-space-batch";
import { SpaceFilterBar } from "./space-filter-bar";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { FolderCardsBar } from "./folder-cards-bar";


interface SpaceBoardProps {
  spaceId: string;
  onWorkflowOpen?: () => void;
}

export function SpaceBoard({ spaceId, onWorkflowOpen }: Readonly<SpaceBoardProps>) {
  const navigate = useNavigate({ from: "/workspaces/$workspaceId/spaces/$spaceId" });
  const search = useSearch({ strict: false }) as { contextPanel?: { type: string; id: string } };
  const selectedItemId = search.contextPanel?.id;

  const containerRef = useRef<HTMLDivElement | null>(null);

  const { isLoading, isFullyLoaded } = useSpaceItemsFullLoad(spaceId);
  const boardItems = useSpaceBoardItems(spaceId);
  const statuses = useSpaceStatuses(spaceId);

  const [hiddenStatusIds, setHiddenStatusIds] = useState<string[]>([]);
  const [hideUnclassified, setHideUnclassified] = useState(false);
  const [filter, setFilter] = useState<SpaceBoardFilter>({});
  const [searchInput, setSearchInput] = useState("");
  const debouncedSearch = useDebounce(searchInput, 300);

  const { workspaceId } = useWorkspace();

  const folders = useSelector((state: RootState) =>
    folderSelectors.selectAll(state)
      .filter(f => f.spaceId === spaceId)
      .sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1))
  );

  const folderTaskCounts = useMemo(() => {
    const counts: Record<string, number> = {};
    boardItems.forEach(item => {
      if (item.folderId) counts[item.folderId] = (counts[item.folderId] || 0) + 1;
    });
    return counts;
  }, [boardItems]);

  const filteredItems = useMemo(() => {
    return boardItems.filter(item => {
      if (filter.priorities?.length && !filter.priorities.includes(item.priority ?? "")) return false;
      if (filter.folderIds?.length) {
        const fid = item.folderId ?? "__none__";
        if (!filter.folderIds.includes(fid)) return false;
      }
      if (debouncedSearch && !item.name.toLowerCase().includes(debouncedSearch.toLowerCase())) return false;
      if (filter.startDate && (!item.startDate || item.startDate < filter.startDate)) return false;
      if (filter.dueDate && (!item.dueDate || item.dueDate > filter.dueDate)) return false;
      return true;
    });
  }, [boardItems, filter, debouncedSearch]);

  const [batchUpdateMutation] = useBatchUpdateSpaceItemsMutation();
  const enqueue = useDebouncedSpaceBatch(batchUpdateMutation, spaceId);

  const columns = useMemo(() => {
    const nextCols: Record<string, BoardItem[]> = {};

    statuses.forEach((s) => { nextCols[s.id] = []; });
    nextCols["unclassified"] = [];

    filteredItems.forEach((item) => {
      const colId = item.statusId && nextCols[item.statusId] ? item.statusId : "unclassified";
      nextCols[colId].push(item);
    });

    Object.keys(nextCols).forEach((colId) => {
      nextCols[colId].sort(prioritySort);
    });

    return nextCols;
  }, [filteredItems, statuses]);

  const { sensors, draggedItem, handleDragStart, handleDragEnd } = useBoardDnd({
    boardItems: filteredItems,
    statuses,
    columns,
    enqueue,
  });

  const isDragging = draggedItem !== null;
  useSmartWheelScroll(containerRef, isDragging);
  useEdgeScroll(containerRef, isDragging);

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

  const handleDateChange = useCallback((itemId: string, patches: { startDate?: string; dueDate?: string; clearStartDate?: boolean; clearDueDate?: boolean }) => {
    enqueue({ id: itemId, type: EntityLayerType.ProjectTask, ...patches });
  }, [enqueue]);

  const handlePriorityChange = useCallback((itemId: string, priority: Priority) => {
    enqueue({ id: itemId, type: EntityLayerType.ProjectTask, priority });
  }, [enqueue]);

  const emptyStatusIds = useMemo(
    () => statuses.filter(s => (columns[s.id]?.length ?? 0) === 0).map(s => s.id),
    [statuses, columns]
  );
  const [prevIsFullyLoaded, setPrevIsFullyLoaded] = useState(isFullyLoaded);
  if (isFullyLoaded !== prevIsFullyLoaded) {
    setPrevIsFullyLoaded(isFullyLoaded);
    if (isFullyLoaded && emptyStatusIds.length > 0) {
      setHiddenStatusIds(prev => [...new Set([...prev, ...emptyStatusIds])]);
    }
  }

  const allEmptyHidden = emptyStatusIds.length > 0 && emptyStatusIds.every(id => hiddenStatusIds.includes(id));

  const handleToggleHideEmpty = useCallback(() => {
    setHiddenStatusIds(prev =>
      allEmptyHidden
        ? prev.filter(id => !emptyStatusIds.includes(id))
        : [...new Set([...prev, ...emptyStatusIds])]
    );
  }, [allEmptyHidden, emptyStatusIds]);

  const columnsToRender = useMemo(() => {
    const cols = statuses
      .filter(s => !hiddenStatusIds.includes(s.id))
      .map(s => ({ id: s.id, name: s.name, color: s.color, category: s.category, items: columns[s.id] || [] }));

    if (!hideUnclassified) {
      cols.push({ id: "unclassified", name: "Unclassified", color: "#6b7280", category: "NotStarted", items: columns["unclassified"] || [] });
    }

    return cols;
  }, [statuses, columns, hiddenStatusIds, hideUnclassified]);

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
        folders={folders}
        filter={filter}
        onFilterChange={setFilter}
        searchInput={searchInput}
        onSearchChange={setSearchInput}
        onWorkflowOpen={onWorkflowOpen}
        isFullyLoaded={isFullyLoaded}
        hideUnclassified={hideUnclassified}
        onToggleUnclassified={() => setHideUnclassified(v => !v)}
        hasEmptyStatuses={emptyStatusIds.length > 0}
        allEmptyHidden={allEmptyHidden}
        onToggleHideEmpty={handleToggleHideEmpty}
      />

      <FolderCardsBar
        spaceId={spaceId}
        workspaceId={workspaceId}
        folders={folders}
        folderTaskCounts={folderTaskCounts}
      />

      <DndContext
        sensors={sensors}
        collisionDetection={closestCorners}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
      >
        <div
          ref={containerRef}
          className="flex-1 flex  gap-2 px-2 overflow-x-auto overflow-y-hidden select-none [&::-webkit-scrollbar]:h-1.5 [&::-webkit-scrollbar-thumb]:bg-white/5 [&::-webkit-scrollbar-thumb]:rounded-full hover:[&::-webkit-scrollbar-thumb]:bg-white/15 [&::-webkit-scrollbar-track]:bg-transparent"
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
              onHide={col.id === "unclassified" ? () => setHideUnclassified(true) : undefined}
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
