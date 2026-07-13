import { useRef, useMemo, useCallback, useState, useEffect } from "react";
import { observer } from "mobx-react-lite";
import { useNavigate, useParams, useSearch } from "@tanstack/react-router";
import { useIsMobile } from "@/hooks/use-mobile";
import { cn } from "@/lib/utils";
import { createPortal } from "react-dom";
import {
  DndContext,
  DragOverlay,
  type CollisionDetection,
} from "@dnd-kit/core";
import { SortableContext, horizontalListSortingStrategy } from "@dnd-kit/sortable";
import { pointerAwareCollisionDetection } from "@/lib/dnd-collision";
import { Priority } from "@/types/priority";
import type { BoardItem, SpaceBoardFilter, SpaceBoardSortBy } from "../space-board-types";
import { getBoardSortComparator } from "../space-board-types";
import { useDebounce } from "@/hooks/use-debounce";
import { BoardItemCard } from "./sortable-board-item";
import { useBoardDnd } from "./use-board-dnd";
import { BoardColumn } from "./board-column";
import { useSmartWheelScroll } from "@/features/workspace/contents/views/space/utils/use-smart-wheel-scroll";
import { useEdgeScroll } from "@/features/workspace/contents/views/space/utils/use-edge-scroll";
import { SpaceFilterBar } from "./space-filter-bar";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine, useSyncReady } from "@/sync/sync-provider";
import { useDebouncedFlush } from "@/sync/use-debounced-flush";
import { TaskMutations } from "@/mutations/task.mutations";
import { StatusMutations } from "@/mutations/status.mutations";
import { RowAction } from "@/types/row-action";
import type { Status } from "@/types/status";
import { toLocalDay } from "@/lib/date-filter";
import { useLocalStorage } from "@/hooks/use-local-storage";
import { HIDE_EMPTY_DEFAULT_KEY } from "./space-settings-dialog";


interface SpaceBoardProps {
  spaceId: string;
  onOpenWorkflow?: () => void;
}

function sortStatuses(statuses: Status[]): Status[] {
  return [...statuses].sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1));
}

const collisionDetectionStrategy: CollisionDetection = (args) => {
  if (args.active.data.current?.type === "column") {
    const columnContainers = args.droppableContainers.filter(
      (container) => container.data.current?.type === "column"
    );
    return pointerAwareCollisionDetection({ ...args, droppableContainers: columnContainers });
  }
  return pointerAwareCollisionDetection(args);
};

export const SpaceBoard = observer(function SpaceBoard({ spaceId, onOpenWorkflow }: Readonly<SpaceBoardProps>) {
  const navigate = useNavigate({ from: "/workspaces/$workspaceId/spaces/$spaceId" });
  const { workspaceId } = useParams({ from: "/workspaces/$workspaceId/spaces/$spaceId" });
  const isMobile = useIsMobile();
  const search = useSearch({ strict: false }) as { contextPanel?: { type: string; id: string } };
  const selectedItemId = search.contextPanel?.id;

  const containerRef = useRef<HTMLDivElement | null>(null);

  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const { ready } = useSyncReady();
  const taskMutations = useMemo(() => new TaskMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const statusMutations = useMemo(() => new StatusMutations(rootStore), [rootStore]);
  const { scheduleFlush } = useDebouncedFlush(syncEngine);

  const boardItems: BoardItem[] = rootStore.taskStore
    .getBySpace(spaceId)
    .filter((t) => !t.parentTaskId)
    .map((t) => ({ ...t, __type: "task" as const }));

  const [hiddenStatusIds, setHiddenStatusIds] = useState<string[]>([]);
  const [hideUnclassified, setHideUnclassified] = useState(false);
  const [filter, setFilter] = useState<SpaceBoardFilter>({});
  const [sortBy, setSortBy] = useState<SpaceBoardSortBy>("priority");
  const [searchInput, setSearchInput] = useState("");
  const debouncedSearch = useDebounce(searchInput, 300);
  const [activeColumnIndex, setActiveColumnIndex] = useState(0);
  const handleBoardScroll = useCallback((e: React.UIEvent<HTMLDivElement>) => {
    const el = e.currentTarget;
    if (el.clientWidth === 0) return;
    setActiveColumnIndex(Math.round(el.scrollLeft / el.clientWidth));
  }, []);

  const statuses = sortStatuses(rootStore.statusStore.getVisibleForSpace(spaceId));

  const filteredItems = useMemo(() => {
    return boardItems.filter(item => {
      if (filter.priorities?.length && !filter.priorities.includes(item.priority ?? "")) return false;
      if (filter.statusIds?.length && !filter.statusIds.includes(item.statusId ?? "")) return false;
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

  const enqueue = useCallback((update: { id: string; statusId?: string | null; priority?: Priority; orderKey?: string; startDate?: string | null; dueDate?: string | null }) => {
    const { id, ...patch } = update;
    taskMutations.updateLocal(id, patch).catch((err) => console.error("Failed to apply local task update", err));
    scheduleFlush();
  }, [taskMutations, scheduleFlush]);

  // Column reorder — reuses the same batch mutation the Workflow Manager dialog uses (optimistic
  // apply + IDB write + rollback-on-failure already built in), invoked with a single-row Update.
  const onColumnReorder = useCallback((statusId: string, orderKey: string) => {
    const status = statuses.find((s) => s.id === statusId);
    if (!status) return;
    statusMutations.updateBatch([
      { id: status.id, name: status.name, color: status.color, orderKey, spaceId: status.spaceId ?? null, action: RowAction.Update },
    ]).catch((err) => console.error("Failed to reorder status column", err));
  }, [statuses, statusMutations]);

  const columns = useMemo(() => {
    const nextCols: Record<string, BoardItem[]> = {};

    statuses.forEach((s) => { nextCols[s.id] = []; });
    nextCols["unclassified"] = [];

    filteredItems.forEach((item) => {
      const colId = item.statusId && nextCols[item.statusId] ? item.statusId : "unclassified";
      nextCols[colId].push(item);
    });

    const comparator = getBoardSortComparator(sortBy);
    Object.keys(nextCols).forEach((colId) => {
      nextCols[colId].sort(comparator);
    });

    return nextCols;
  }, [filteredItems, statuses, sortBy]);

  const { sensors, draggedItem, handleDragStart, handleDragEnd } = useBoardDnd({
    boardItems: filteredItems,
    statuses,
    columns,
    enqueue,
    onColumnReorder,
  });

  const isDragging = draggedItem !== null;
  useSmartWheelScroll(containerRef, isDragging);
  useEdgeScroll(containerRef, isDragging);

  const handleTaskClick = useCallback((id: string) => {
    // Mobile has no side context-panel to open — go straight to the task's full page instead.
    if (isMobile) {
      navigate({ to: "/workspaces/$workspaceId/tasks/$taskId", params: { workspaceId, taskId: id } });
      return;
    }

    navigate({
      search: (prev) => {
        const searchParams = prev as Record<string, unknown>;
        return {
          ...searchParams,
          contextPanel: { type: "task", id }
        };
      }
    });
  }, [navigate, isMobile, workspaceId]);

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

  // Hide-empty is the default state (configurable in Space Settings), applied once as soon as
  // the board's data is fully loaded — a ref (not a ready/prevReady state comparison) so it
  // still fires even when `ready` is already true on mount, not just on a false→true transition.
  const [hideEmptyDefault] = useLocalStorage(HIDE_EMPTY_DEFAULT_KEY, true);
  const didAutoHideEmpty = useRef(false);
  useEffect(() => {
    if (!ready || didAutoHideEmpty.current || !hideEmptyDefault) return;
    didAutoHideEmpty.current = true;
    if (emptyStatusIds.length > 0) {
      setHiddenStatusIds(prev => [...new Set([...prev, ...emptyStatusIds])]);
    }
    if ((columns["unclassified"]?.length ?? 0) === 0) {
      setHideUnclassified(true);
    }
  }, [ready, emptyStatusIds, columns, hideEmptyDefault]);

  const columnsToRender = useMemo(() => {
    const cols = statuses
      .filter(s => !hiddenStatusIds.includes(s.id))
      .map(s => ({ id: s.id, name: s.name, color: s.color, items: columns[s.id] || [] }));

    if (!hideUnclassified) {
      cols.push({ id: "unclassified", name: "Unclassified", color: "#6b7280", items: columns["unclassified"] || [] });
    }

    return cols;
  }, [statuses, columns, hiddenStatusIds, hideUnclassified]);

  const draggableColumnIds = useMemo(
    () => columnsToRender.filter(c => c.id !== "unclassified").map(c => c.id),
    [columnsToRender]
  );

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
        filter={filter}
        onFilterChange={setFilter}
        searchInput={searchInput}
        onSearchChange={setSearchInput}
        isFullyLoaded={ready}
        hideUnclassified={hideUnclassified}
        onToggleUnclassified={() => setHideUnclassified(v => !v)}
        onOpenWorkflow={onOpenWorkflow}
        sortBy={sortBy}
        onSortByChange={setSortBy}
      />

      {isMobile && columnsToRender.length > 1 && (
        <div className="flex gap-1 px-2 pb-1.5 overflow-x-auto shrink-0 [&::-webkit-scrollbar]:hidden">
          {columnsToRender.map((col, index) => (
            <button
              key={col.id}
              type="button"
              onClick={() => {
                const el = containerRef.current;
                if (!el) return;
                el.scrollTo({ left: index * el.clientWidth, behavior: "smooth" });
              }}
              className={cn(
                "shrink-0 h-6 px-2.5 rounded-full text-[10px] font-semibold border transition-colors",
                index === activeColumnIndex
                  ? "bg-primary/10 text-primary border-primary/30"
                  : "text-muted-foreground border-border/40",
              )}
            >
              {col.name} · {col.items.length}
            </button>
          ))}
        </div>
      )}

      <DndContext
        sensors={sensors}
        collisionDetection={collisionDetectionStrategy}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
      >
        <div
          ref={containerRef}
          onScroll={isMobile ? handleBoardScroll : undefined}
          className={cn(
            "flex-1 flex gap-2 px-2 overflow-x-auto overflow-y-hidden select-none [&::-webkit-scrollbar]:h-1.5 [&::-webkit-scrollbar-thumb]:bg-white/5 [&::-webkit-scrollbar-thumb]:rounded-full hover:[&::-webkit-scrollbar-thumb]:bg-white/15 [&::-webkit-scrollbar-track]:bg-transparent",
            isMobile && "snap-x snap-mandatory",
          )}
        >
          <SortableContext items={draggableColumnIds} strategy={horizontalListSortingStrategy}>
            {columnsToRender.map((col) => (
              <BoardColumn
                key={col.id}
                statusId={col.id}
                name={col.name}
                color={col.color}
                items={col.items}
                spaceId={spaceId}
                selectedItemId={selectedItemId}
                onTaskClick={handleTaskClick}
                onPriorityChange={handlePriorityChange}
                onDateChange={handleDateChange}
                onHide={col.id === "unclassified" ? () => setHideUnclassified(true) : undefined}
                draggable={col.id !== "unclassified"}
                fullWidth={isMobile}
              />
            ))}
          </SortableContext>
        </div>

        {createPortal(
          <DragOverlay dropAnimation={null}>
            {draggedItem ? (
              // Add layout-stable inline styles to prevent container shifting on mount
              <div
                className="rotate-3 scale-105 opacity-90 pointer-events-none w-67 contain-layout"
                style={{ willChange: "transform" }}
              >
                <BoardItemCard item={draggedItem} isDragging={false} showActions={false} />
              </div>
            ) : null}
          </DragOverlay>,
          document.body
        )}
      </DndContext>
    </>
  );
});
