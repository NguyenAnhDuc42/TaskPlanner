import { useState, useRef, useEffect } from "react";
import {
  DndContext,
  useSensor,
  useSensors,
  PointerSensor,
  TouchSensor,
  type DragStartEvent,
  type DragEndEvent,
  DragOverlay,
  useDroppable,
} from "@dnd-kit/core";
import {
  SortableContext,
  useSortable,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { cn } from "@/lib/utils";
import { StatusGroup } from "../components/items/status-group";
import { TaskItem } from "../components/items/task-item";
import { FolderItem } from "../components/items/folder-item";
import { useSmartWheelScroll } from "../components/board/use-smart-wheel-scroll";
import { useEdgeScroll } from "../components/board/use-edge-scroll";
import { StatusCategory } from "@/types/status-category";

interface UnifiedBoardViewProps {
  columns: Record<string, any[]>;
  statuses: any[];
  onMove: (event: {
    activeId: string;
    targetStatusId: string | undefined;
    targetIndex: number;
    previousItemOrderKey: string | undefined;
    nextItemOrderKey: string | undefined;
  }) => void;
  onTaskClick: (taskId: string) => void;
  onFolderClick: (folderId: string) => void;
}

// 1. Draggable Wrapper using `@dnd-kit/sortable`
function SortableItem({
  item,
  type,
  statusId,
  onTaskClick,
  onFolderClick,
}: {
  item: any;
  type: "task" | "folder";
  statusId: string;
  onTaskClick: (id: string) => void;
  onFolderClick: (id: string) => void;
}) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({
    id: item.id,
    data: { item, type, statusId },
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.3 : 1,
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...attributes}
      {...listeners}
      className="outline-none"
    >
      {type === "task" ? (
        <TaskItem task={item} onClick={() => onTaskClick(item.id)} />
      ) : (
        <FolderItem folder={item} onClick={() => onFolderClick(item.id)} />
      )}
    </div>
  );
}

// 2. Droppable Column Wrapper
function BoardColumn({
  status,
  items,
  onTaskClick,
  onFolderClick,
}: {
  status: any;
  items: any[];
  onTaskClick: (id: string) => void;
  onFolderClick: (id: string) => void;
}) {
  const { setNodeRef, isOver } = useDroppable({
    id: status.statusId,
  });

  const itemIds = items.map((i) => i.id);

  return (
    <StatusGroup
      id={status.statusId}
      statusName={status.name}
      color={status.color}
      totalCount={items.length}
      className="w-[300px]"
    >
      <div
        ref={setNodeRef}
        className={cn(
          "flex flex-col h-[calc(100vh-250px)] overflow-y-auto p-1 gap-2 rounded-md transition-colors status-column-scrollable",
          "[&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/10 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/30 [&::-webkit-scrollbar-track]:bg-transparent",
          isOver
            ? "bg-white/[0.02] border border-dashed border-border/60"
            : "border border-transparent",
        )}
      >
        <SortableContext items={itemIds} strategy={verticalListSortingStrategy}>
          {items.map((item) => {
            const isTask = item.__type === "task";
            return (
              <SortableItem
                key={item.id}
                item={item}
                type={isTask ? "task" : "folder"}
                statusId={status.statusId}
                onTaskClick={onTaskClick}
                onFolderClick={onFolderClick}
              />
            );
          })}
        </SortableContext>
      </div>
    </StatusGroup>
  );
}

// 3. Main Presenter
export function UnifiedBoardView({
  columns,
  statuses,
  onMove,
  onTaskClick,
  onFolderClick,
}: UnifiedBoardViewProps) {
  const [activeItem, setActiveItem] = useState<any | null>(null);
  const [activeType, setActiveType] = useState<"task" | "folder" | null>(null);

  const [boardColumns, setBoardColumns] =
    useState<Record<string, any[]>>(columns);
  const containerRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (!activeItem) {
      setBoardColumns(columns);
    }
  }, [columns, activeItem]);

  // Activation constraints to make sure normal clicks trigger click handlers
  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 5,
      },
    }),
    useSensor(TouchSensor, {
      activationConstraint: {
        delay: 250,
        tolerance: 5,
      },
    }),
  );

  const isDragging = activeItem !== null;
  useSmartWheelScroll(containerRef, isDragging);
  useEdgeScroll(containerRef, isDragging);

  const displayStatuses = [...statuses];
  const unclassifiedItems = boardColumns["unclassified"] ?? [];
  if (unclassifiedItems.length > 0) {
    displayStatuses.push({
      statusId: "unclassified",
      name: "Unclassified",
      color: "#6b7280",
      category: StatusCategory.NotStarted,
      orderKey: "",
    });
  }

  function handleDragStart(event: DragStartEvent) {
    const { active } = event;
    const data = active.data.current;
    if (data) {
      setActiveItem(data.item);
      setActiveType(data.type);
    }
  }

  function handleDragOver(event: any) {
    const { active, over } = event;
    if (!over) return;

    const activeId = active.id as string;
    const overId = over.id as string;

    const activeColId = Object.keys(boardColumns).find((key) =>
      boardColumns[key].some((item) => item.id === activeId),
    );
    const overColId =
      statuses.some((s) => s.statusId === overId) || overId === "unclassified"
        ? overId
        : Object.keys(boardColumns).find((key) =>
            boardColumns[key].some((item) => item.id === overId),
          );

    if (!activeColId || !overColId) {
      return;
    }

    if (activeColId === overColId) {
      setBoardColumns((prev) => {
        const colItems = prev[activeColId] ?? [];
        const activeIndex = colItems.findIndex((item) => item.id === activeId);
        const overIndex = colItems.findIndex((item) => item.id === overId);

        if (
          activeIndex === -1 ||
          overIndex === -1 ||
          activeIndex === overIndex
        ) {
          return prev;
        }

        const newColItems = [...colItems];
        const [movedItem] = newColItems.splice(activeIndex, 1);
        newColItems.splice(overIndex, 0, movedItem);

        return {
          ...prev,
          [activeColId]: newColItems,
        };
      });
      return;
    }

    setBoardColumns((prev) => {
      const activeItems = prev[activeColId] ?? [];
      const overItems = prev[overColId] ?? [];

      const activeIndex = activeItems.findIndex((item) => item.id === activeId);
      if (activeIndex === -1) return prev;

      const overIndex = overItems.findIndex((item) => item.id === overId);
      let targetIndex = overIndex === -1 ? overItems.length : overIndex;

      const newActiveItems = [...activeItems];
      const [movedItem] = newActiveItems.splice(activeIndex, 1);

      const newOverItems = [...overItems];
      newOverItems.splice(targetIndex, 0, {
        ...movedItem,
        statusId: overColId === "unclassified" ? undefined : overColId,
      });

      return {
        ...prev,
        [activeColId]: newActiveItems,
        [overColId]: newOverItems,
      };
    });
  }

  function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    setActiveItem(null);
    setActiveType(null);

    if (!over) return;

    const activeId = active.id as string;

    // Find the final column and index in boardColumns
    const targetStatusId = Object.keys(boardColumns).find((key) =>
      boardColumns[key].some((item) => item.id === activeId),
    );
    if (!targetStatusId) return;

    const targetColItems = boardColumns[targetStatusId] ?? [];
    const targetIndex = targetColItems.findIndex(
      (item) => item.id === activeId,
    );

    const resolvedStatusId =
      targetStatusId === "unclassified" ? undefined : targetStatusId;

    // Calculate boundary order keys using original columns to avoid using mutated keys
    const stripped = (columns[targetStatusId] ?? []).filter(
      (i) => i.id !== activeId,
    );
    const clampedIndex = Math.max(0, Math.min(targetIndex, stripped.length));
    const previousItemOrderKey = stripped[clampedIndex - 1]?.orderKey;
    const nextItemOrderKey = stripped[clampedIndex]?.orderKey;

    onMove({
      activeId,
      targetStatusId: resolvedStatusId,
      targetIndex,
      previousItemOrderKey,
      nextItemOrderKey,
    });
  }

  return (
    <DndContext
      sensors={sensors}
      onDragStart={handleDragStart}
      onDragOver={handleDragOver}
      onDragEnd={handleDragEnd}
    >
      <div
        ref={containerRef}
        className="h-full flex gap-4 p-6 overflow-x-auto overflow-y-hidden animate-in fade-in slide-in-from-bottom-2 duration-500 [&::-webkit-scrollbar]:h-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/10 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/30 [&::-webkit-scrollbar-track]:bg-transparent"
      >
        {displayStatuses.map((status) => {
          const items = boardColumns[status.statusId] ?? [];
          return (
            <BoardColumn
              key={status.statusId}
              status={status}
              items={items}
              onTaskClick={onTaskClick}
              onFolderClick={onFolderClick}
            />
          );
        })}
      </div>

      <DragOverlay dropAnimation={null}>
        {activeItem && activeType ? (
          <div className="rotate-3 scale-105 opacity-90 cursor-grabbing pointer-events-none">
            {activeType === "task" ? (
              <TaskItem task={activeItem} onClick={() => {}} />
            ) : (
              <FolderItem folder={activeItem} onClick={() => {}} />
            )}
          </div>
        ) : null}
      </DragOverlay>
    </DndContext>
  );
}
