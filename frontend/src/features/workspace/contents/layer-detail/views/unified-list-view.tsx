import { useState, useRef } from "react";
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
  verticalListSortingStrategy
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { cn } from "@/lib/utils";
import { format } from "date-fns";
import { Layers, Clock, User, Folder as FolderIcon } from "lucide-react";
import { StatusGroup } from "../components/items/status-group";

import { Priority } from "@/types/priority";
import { StatusCategory } from "@/types/status-category";
import { useEdgeScroll } from "./use-edge-scroll";
import { PriorityBadge } from "@/components/priority-badge";

interface UnifiedListViewProps {
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

// 1. Sortable Row Component
function SortableRow({
  item,
  type,
  statusId,
  isFirst,
  onTaskClick,
  onFolderClick
}: {
  item: any;
  type: "task" | "folder";
  statusId: string;
  isFirst?: boolean;
  onTaskClick: (id: string) => void;
  onFolderClick: (id: string) => void;
}) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging
  } = useSortable({
    id: item.id,
    data: { item, type, statusId }
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.3 : 1
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
        <TaskListRow
          task={item}
          isFirst={isFirst}
          onClick={() => onTaskClick(item.id)}
        />
      ) : (
        <FolderListRow
          folder={item}
          isFirst={isFirst}
          onClick={() => onFolderClick(item.id)}
        />
      )}
    </div>
  );
}

// 2. Droppable List Area
function ListGroupArea({
  status,
  items,
  onTaskClick,
  onFolderClick
}: {
  status: any;
  items: any[];
  onTaskClick: (id: string) => void;
  onFolderClick: (id: string) => void;
}) {
  const { setNodeRef, isOver } = useDroppable({
    id: status.statusId
  });

  const itemIds = items.map((i) => i.id);

  return (
    <StatusGroup
      id={status.statusId}
      statusName={status.name}
      color={status.color}
      totalCount={items.length}
      className="w-full"
    >
      <div
        ref={setNodeRef}
        className={cn(
          "flex flex-col min-h-[40px] rounded-md transition-colors p-1",
          isOver ? "bg-white/[0.01] border border-dashed border-border/40" : "border border-transparent"
        )}
      >
        <SortableContext items={itemIds} strategy={verticalListSortingStrategy}>
          {items.map((item, idx) => {
            const isTask = "priority" in item;
            return (
              <SortableRow
                key={item.id}
                item={item}
                type={isTask ? "task" : "folder"}
                statusId={status.statusId}
                isFirst={idx === 0}
                onTaskClick={onTaskClick}
                onFolderClick={onFolderClick}
              />
            );
          })}
        </SortableContext>

        {items.length === 0 && (
          <div className="h-12 flex items-center justify-center text-xs text-[#4a4b53]">
            No items in this section
          </div>
        )}
      </div>
    </StatusGroup>
  );
}

// 3. Presenter Component
export function UnifiedListView({
  columns,
  statuses,
  onMove,
  onTaskClick,
  onFolderClick
}: UnifiedListViewProps) {
  const [activeItem, setActiveItem] = useState<any | null>(null);
  const [activeType, setActiveType] = useState<"task" | "folder" | null>(null);

  const containerRef = useRef<HTMLDivElement | null>(null);

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 8
      }
    }),
    useSensor(TouchSensor, {
      activationConstraint: {
        delay: 250,
        tolerance: 5
      }
    })
  );

  const isDragging = activeItem !== null;
  useEdgeScroll(containerRef, isDragging, true); // Active vertical scrolling for List Mode!

  const displayStatuses = [...statuses];
  const unclassifiedItems = columns["unclassified"] ?? [];
  if (unclassifiedItems.length > 0) {
    displayStatuses.push({
      statusId: "unclassified",
      name: "Unclassified",
      color: "#6b7280",
      category: StatusCategory.NotStarted,
      orderKey: ""
    } as any);
  }

  function handleDragStart(event: DragStartEvent) {
    const { active } = event;
    const data = active.data.current;
    if (data) {
      setActiveItem(data.item);
      setActiveType(data.type);
    }
  }

  function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    setActiveItem(null);
    setActiveType(null);

    if (!over) return;

    const activeId = active.id as string;
    let targetStatusId: string = over.id as string;
    let targetIndex = 0;

    const isOverColumn = statuses.some(s => s.statusId === over.id) || over.id === "unclassified";
    const targetItems = columns[targetStatusId] ?? [];

    if (isOverColumn) {
      targetIndex = targetItems.filter(i => i.id !== activeId).length;
    } else {
      const overData = over.data.current;
      if (overData) {
        targetStatusId = overData.statusId;
        const colItems = columns[targetStatusId] ?? [];
        const stripped = colItems.filter(i => i.id !== activeId);
        targetIndex = stripped.findIndex(i => i.id === over.id);
        if (targetIndex === -1) {
          targetIndex = stripped.length;
        }
      }
    }

    const resolvedStatusId = targetStatusId === "unclassified" ? undefined : targetStatusId;

    const stripped = (columns[targetStatusId] ?? []).filter(i => i.id !== activeId);
    const clampedIndex = Math.max(0, Math.min(targetIndex, stripped.length));
    const previousItemOrderKey = stripped[clampedIndex - 1]?.orderKey;
    const nextItemOrderKey = stripped[clampedIndex]?.orderKey;

    onMove({
      activeId,
      targetStatusId: resolvedStatusId,
      targetIndex,
      previousItemOrderKey,
      nextItemOrderKey
    });
  }

  return (
    <DndContext
      sensors={sensors}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
    >
      <div
        ref={containerRef}
        className="h-full flex flex-col bg-[#0a0a0b] overflow-y-auto no-scrollbar"
      >
        <div className="p-6 space-y-8">
          {displayStatuses.map((status) => {
            const items = columns[status.statusId] ?? [];
            return (
              <ListGroupArea
                key={status.statusId}
                status={status}
                items={items}
                onTaskClick={onTaskClick}
                onFolderClick={onFolderClick}
              />
            );
          })}
        </div>
      </div>

      <DragOverlay dropAnimation={null}>
        {activeItem && activeType ? (
          <div className="scale-[1.02] opacity-90 cursor-grabbing pointer-events-none w-[calc(100%-48px)] shadow-2xl">
            {activeType === "task" ? (
              <TaskListRow task={activeItem} onClick={() => {}} />
            ) : (
              <FolderListRow folder={activeItem} onClick={() => {}} />
            )}
          </div>
        ) : null}
      </DragOverlay>
    </DndContext>
  );
}

// 4. Row Renderers
function TaskListRow({
  task,
  onClick,
  isFirst,
}: {
  task: any;
  onClick: () => void;
  isFirst?: boolean;
}) {
  return (
    <div
      onClick={onClick}
      className={cn(
        "h-10 px-4 flex items-center gap-4 hover:bg-[#1c1c1f] transition-colors cursor-pointer border-t border-[#202127]",
        isFirst && "border-t-0",
      )}
    >
      <div className="flex items-center gap-3 shrink-0">
        <PriorityBadge priority={task.priority as Priority} />
        <span className="text-xs font-medium text-[#4a4b53] tracking-wider min-w-[50px]">
          {`T-${task.id.slice(0, 4).toUpperCase()}`}
        </span>
      </div>

      <div className="flex-1 min-w-0">
        <span className="text-sm font-medium text-[#f5f5f7] truncate block">
          {task.name}
        </span>
      </div>

      <div className="flex items-center gap-4 shrink-0">
        {task.dueDate && (
          <div className="flex items-center gap-1.5 text-[#4a4b53]">
            <Clock className="h-3.5 w-3.5" />
            <span className="text-xs font-medium">
              {format(new Date(task.dueDate), "MMM d")}
            </span>
          </div>
        )}

        <div className="h-5 w-5 rounded-full bg-[#202127] flex items-center justify-center border border-[#2c2d35]">
          <User className="h-3 w-3 text-[#8a8b94]" />
        </div>
      </div>
    </div>
  );
}

function FolderListRow({
  folder,
  onClick,
  isFirst,
}: {
  folder: any;
  onClick: () => void;
  isFirst?: boolean;
}) {
  const folderColor = folder.color || "#3b82f6";

  return (
    <div
      onClick={onClick}
      className={cn(
        "h-10 px-4 flex items-center gap-4 hover:bg-[#1c1c1f] transition-colors cursor-pointer border-t border-[#202127] bg-[#141416]",
        isFirst && "border-t-0",
      )}
    >
      <div className="flex items-center gap-3 shrink-0">
        <Layers className="h-3.5 w-3.5 text-[#3b82f6]" />
        <span className="text-xs font-medium text-[#4a4b53] tracking-wider">
          FOLDER
        </span>
      </div>

      <div className="flex-1 min-w-0 flex items-center gap-2">
        <div
          className="shrink-0 h-5 w-5 rounded flex items-center justify-center border border-[#2c2d35]"
          style={{
            backgroundColor: `${folderColor}20`,
            color: folderColor,
          }}
        >
          {folder.icon ? (
            <span className="text-xs">{folder.icon}</span>
          ) : (
            <FolderIcon className="h-3 w-3" />
          )}
        </div>
        <span className="text-sm font-medium text-[#f5f5f7] truncate block">
          {folder.name}
        </span>
      </div>

      <div className="flex items-center gap-4 shrink-0">
        <div className="h-5 w-5 rounded-full bg-[#202127] flex items-center justify-center border border-[#2c2d35]">
          <User className="h-3 w-3 text-[#8a8b94]" />
        </div>
      </div>
    </div>
  );
}
