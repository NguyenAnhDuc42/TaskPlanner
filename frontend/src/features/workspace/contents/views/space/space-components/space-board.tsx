import { useRef, useMemo, useCallback, useState } from "react";
import { observer } from "mobx-react-lite";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { createPortal } from "react-dom";
import {
  DndContext,
  DragOverlay,
  closestCorners
} from "@dnd-kit/core";
import { Priority, prioritySort } from "@/types/priority";
import type { BoardItem, SpaceBoardFilter } from "../space-board-types";
import { useDebounce } from "@/hooks/use-debounce";
import { BoardItemCard } from "./sortable-board-item";
import { useBoardDnd } from "./use-board-dnd";
import { BoardColumn } from "./board-column";
import { useSmartWheelScroll } from "@/features/workspace/contents/views/space/utils/use-smart-wheel-scroll";
import { useEdgeScroll } from "@/features/workspace/contents/views/space/utils/use-edge-scroll";
import { SpaceFilterBar } from "./space-filter-bar";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { FolderCardsBar } from "./folder-cards-bar";
import { useStore } from "@/stores/root.store";
import { useSyncEngine, useSyncReady } from "@/sync/sync-provider";
import { useDebouncedFlush } from "@/sync/use-debounced-flush";
import { TaskMutations } from "@/mutations/task.mutations";
import { StatusCategory } from "@/types/status-category";
import type { Status } from "@/types/status";
import { toLocalDay } from "@/lib/date-filter";


interface SpaceBoardProps {
  spaceId: string;
  onWorkflowOpen?: () => void;
}

const statusCategoryWeight: Record<string, number> = {
  [StatusCategory.NotStarted]: 0,
  [StatusCategory.Active]: 1,
  [StatusCategory.Done]: 2,
  [StatusCategory.Closed]: 3,
};

function sortStatuses(statuses: Status[]): Status[] {
  return [...statuses].sort((a, b) => {
    const weightA = statusCategoryWeight[a.category] ?? 4;
    const weightB = statusCategoryWeight[b.category] ?? 4;
    if (weightA !== weightB) return weightA - weightB;
    return ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1);
  });
}

export const SpaceBoard = observer(function SpaceBoard({ spaceId, onWorkflowOpen }: Readonly<SpaceBoardProps>) {
  const navigate = useNavigate({ from: "/workspaces/$workspaceId/spaces/$spaceId" });
  const search = useSearch({ strict: false }) as { contextPanel?: { type: string; id: string } };
  const selectedItemId = search.contextPanel?.id;

  const containerRef = useRef<HTMLDivElement | null>(null);

  const rootStore = useStore();
  const syncEngine = useSyncEngine();
  const { ready } = useSyncReady();
  const taskMutations = useMemo(() => new TaskMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const { scheduleFlush } = useDebouncedFlush(syncEngine);

  // Tasks/statuses are already fully hydrated locally (Bootstrap + Delta) — plain reads, not
  // useMemo. This component is a mobx-react-lite observer, which tracks observable reads made
  // directly during render; a store read wrapped in useMemo (keyed on the store's object
  // reference, which never changes) would silently freeze — see the folder-view.tsx fix.
  const boardItems: BoardItem[] = rootStore.taskStore
    .getBySpace(spaceId)
    .filter((t) => !t.parentTaskId)
    .map((t) => ({
      ...t,
      __type: "task" as const,
      folderName: t.folderId ? rootStore.folderStore.getById(t.folderId)?.name : undefined,
    }));
  const statuses = sortStatuses(rootStore.statusStore.getBySpace(spaceId));

  const [hiddenStatusIds, setHiddenStatusIds] = useState<string[]>([]);
  const [hideUnclassified, setHideUnclassified] = useState(false);
  const [filter, setFilter] = useState<SpaceBoardFilter>({});
  const [searchInput, setSearchInput] = useState("");
  const debouncedSearch = useDebounce(searchInput, 300);

  const { workspaceId } = useWorkspace();

  const folders = rootStore.folderStore.getBySpace(spaceId).sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1));

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
      if (filter.startDate) {
        const day = toLocalDay(item.startDate);
        if (!day || day < toLocalDay(filter.startDate)!) return false;
      }
      if (filter.dueDate) {
        const day = toLocalDay(item.dueDate);
        if (!day || day > toLocalDay(filter.dueDate)!) return false;
      }
      return true;
    });
  }, [boardItems, filter, debouncedSearch]);

  // Each board mutation (drag-drop, priority change, date change) queues a single local update
  // (store + IndexedDB + enqueue) then debounces the network flush — TransactionQueue.squash()
  // merges rapid successive updates to the same task into one send.
  const enqueue = useCallback((update: { id: string; statusId?: string | null; priority?: Priority; orderKey?: string; startDate?: string | null; dueDate?: string | null }) => {
    const { id, ...patch } = update;
    taskMutations.updateLocal(id, patch).catch((err) => console.error("Failed to apply local task update", err));
    scheduleFlush();
  }, [taskMutations, scheduleFlush]);

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

  const handleDateChange = useCallback((itemId: string, patches: { startDate?: string | null; dueDate?: string | null }) => {
    enqueue({ id: itemId, ...patches });
  }, [enqueue]);

  const handlePriorityChange = useCallback((itemId: string, priority: Priority) => {
    enqueue({ id: itemId, priority });
  }, [enqueue]);

  const emptyStatusIds = useMemo(
    () => statuses.filter(s => (columns[s.id]?.length ?? 0) === 0).map(s => s.id),
    [statuses, columns]
  );
  const [prevIsFullyLoaded, setPrevIsFullyLoaded] = useState(ready);
  if (ready !== prevIsFullyLoaded) {
    setPrevIsFullyLoaded(ready);
    if (ready && emptyStatusIds.length > 0) {
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

  if (!ready && boardItems.length === 0) {
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
        isFullyLoaded={ready}
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
});
