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
import { DynamicIcon } from "@/components/dynamic-icon";
import { Plus } from "lucide-react";
import { toast } from "sonner";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useCreateFolderMutation, useDeleteFolderMutation } from "@/features/workspace/contents/hierarchy/hierarchy-api";
import { extractErrorMessage } from "@/types/api-error";
import { ContextMenu, ContextMenuContent, ContextMenuItem, ContextMenuSeparator, ContextMenuTrigger } from "@/components/ui/context-menu";


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
  const [createFolder] = useCreateFolderMutation();
  const [deleteFolder] = useDeleteFolderMutation();
  const [isCreatingFolder, setIsCreatingFolder] = useState(false);
  const [newFolderName, setNewFolderName] = useState("");
  const folderCreateSubmittedRef = useRef(false);

  const folders = useSelector((state: RootState) =>
    folderSelectors.selectAll(state).filter(f => f.spaceId === spaceId)
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

      <div className="flex items-center gap-1.5 px-2 py-1.5 border-b border-border/15 overflow-x-auto shrink-0 [&::-webkit-scrollbar]:hidden">
        {folders.map(folder => (
          <ContextMenu key={folder.id}>
            <ContextMenuTrigger asChild>
              <button
                type="button"
                onClick={() => navigate({
                  to: "/workspaces/$workspaceId/folders/$folderId",
                  params: { workspaceId, folderId: folder.id }
                })}
                className="flex items-center gap-1.5 px-2.5 py-1 rounded-md border border-border/30 bg-card hover:border-border/60 hover:bg-muted/30 transition-all shrink-0 cursor-pointer group"
              >
                <DynamicIcon name={folder.icon || "Folder"} size={11} color={folder.color || "#6366f1"} />
                <span className="text-[11px] font-medium text-foreground/80 group-hover:text-foreground">{folder.name}</span>
                <span className="text-[9px] font-mono text-muted-foreground/50 bg-muted/40 px-1 rounded-sm">
                  {folderTaskCounts[folder.id] ?? 0}
                </span>
              </button>
            </ContextMenuTrigger>
            <ContextMenuContent className="w-44 bg-popover border-border/50 shadow-2xl rounded-xl p-1.5">
              <ContextMenuItem className="gap-2 cursor-pointer text-xs" onSelect={() => navigate({
                to: "/workspaces/$workspaceId/folders/$folderId",
                params: { workspaceId, folderId: folder.id }
              })}>
                <DynamicIcon name={folder.icon || "Folder"} size={12} color={folder.color || "#6366f1"} />
                <span>Open</span>
              </ContextMenuItem>
              <ContextMenuSeparator />
              <ContextMenuItem
                variant="destructive"
                className="gap-2 cursor-pointer text-xs"
                onSelect={() => deleteFolder({ workspaceId, folderId: folder.id })
                  .unwrap()
                  .catch(err => toast.error(extractErrorMessage(err, "Failed to delete folder")))
                }
              >
                <span>Delete Folder</span>
              </ContextMenuItem>
            </ContextMenuContent>
          </ContextMenu>
        ))}

        {/* Inline create folder */}
        {isCreatingFolder ? (
          <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-md border border-primary/40 bg-primary/5 shrink-0">
            <DynamicIcon name="Folder" size={11} color="#6366f1" />
            <input
              autoFocus
              type="text"
              value={newFolderName}
              onChange={e => setNewFolderName(e.target.value)}
              onKeyDown={e => {
                if (e.key === "Escape") { setIsCreatingFolder(false); setNewFolderName(""); }
                if (e.key === "Enter") e.currentTarget.blur();
              }}
              onBlur={() => {
                if (folderCreateSubmittedRef.current) return;
                folderCreateSubmittedRef.current = true;
                const name = newFolderName.trim();
                setIsCreatingFolder(false);
                setNewFolderName("");
                if (name) {
                  createFolder({ workspaceId, body: { spaceId, name, color: "#6366f1", icon: "Folder" } })
                    .unwrap()
                    .catch(err => toast.error(extractErrorMessage(err, "Failed to create folder")));
                }
                setTimeout(() => { folderCreateSubmittedRef.current = false; }, 300);
              }}
              className="text-[11px] font-medium bg-transparent border-none outline-none w-24 text-foreground"
              placeholder="Folder name..."
            />
          </div>
        ) : (
          <button
            type="button"
            onClick={() => setIsCreatingFolder(true)}
            className="flex items-center gap-1 px-2 py-1 rounded-md text-muted-foreground/50 hover:text-muted-foreground hover:bg-muted/30 transition-all shrink-0 cursor-pointer"
          >
            <Plus className="h-3 w-3" />
            <span className="text-[10px] font-medium">New</span>
          </button>
        )}
      </div>

      <DndContext
        sensors={sensors}
        collisionDetection={closestCorners}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
      >
        <div
          ref={containerRef}
          className="flex-1 flex pt-1 gap-2 px-2 overflow-x-auto overflow-y-hidden select-none [&::-webkit-scrollbar]:h-1.5 [&::-webkit-scrollbar-thumb]:bg-white/5 [&::-webkit-scrollbar-thumb]:rounded-full hover:[&::-webkit-scrollbar-thumb]:bg-white/15 [&::-webkit-scrollbar-track]:bg-transparent"
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
