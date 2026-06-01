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
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { cn } from "@/lib/utils";
import { format } from "date-fns";
import { Clock, User } from "lucide-react";
import { StatusGroup } from "../components/items/status-group";

import { Priority } from "@/types/priority";
import { StatusCategory } from "@/types/status-category";
import { useEdgeScroll } from "../../views/space/utils/use-edge-scroll";
import { InlinePriorityPicker } from "../components/items/inline-priority-picker";
import { DynamicIcon } from "@/components/dynamic-icon";
import type { LayerItem, TaskLayerItem, FolderLayerItem } from "../layer-detail-types";
import { useItemsStore } from "../hooks/use-items-store";

interface UnifiedListViewProps {
  columns: Record<string, LayerItem[]>;
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
  onPriorityChange?: (itemId: string, priority: Priority) => void;
}

// 1. Sortable Row Component
function SortableRow({
  item,
  statusId,
  isFirst,
  onTaskClick,
  onFolderClick,
  onPriorityChange,
}: {
  item: LayerItem;
  statusId: string;
  isFirst?: boolean;
  onTaskClick: (id: string) => void;
  onFolderClick: (id: string) => void;
  onPriorityChange?: (itemId: string, priority: Priority) => void;
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
    data: { item, type: item.__type, statusId },
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
      {item.__type === "task" ? (
        <TaskListRow
          task={item as TaskLayerItem}
          isFirst={isFirst}
          onClick={() => onTaskClick(item.id)}
          onPriorityChange={onPriorityChange}
        />
      ) : (
        <FolderListRow
          folder={item as FolderLayerItem}
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
  onFolderClick,
  onPriorityChange,
}: {
  status: any;
  items: LayerItem[];
  onTaskClick: (id: string) => void;
  onFolderClick: (id: string) => void;
  onPriorityChange?: (itemId: string, priority: Priority) => void;
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
      className="w-full"
    >
      <div
        ref={setNodeRef}
        className={cn(
          "flex flex-col min-h-10 rounded-md transition-colors p-1",
          isOver
            ? "bg-white/1 border border-dashed border-border/40"
            : "border border-transparent",
        )}
      >
        <SortableContext items={itemIds} strategy={verticalListSortingStrategy}>
          {items.map((item, idx) => {
            return (
              <SortableRow
                key={item.id}
                item={item}
                statusId={status.statusId}
                isFirst={idx === 0}
                onTaskClick={onTaskClick}
                onFolderClick={onFolderClick}
                onPriorityChange={onPriorityChange}
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
  onFolderClick,
  onPriorityChange,
}: UnifiedListViewProps) {
  const [activeItem, setActiveItem] = useState<any | null>(null);
  const [activeType, setActiveType] = useState<"task" | "folder" | null>(null);
  const containerRef = useRef<HTMLDivElement | null>(null);

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 8,
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
  useEdgeScroll(containerRef, isDragging, true);

  const displayStatuses = [...statuses];
  const unclassifiedItems = columns["unclassified"] ?? [];
  if (unclassifiedItems.length > 0) {
    displayStatuses.push({
      statusId: "unclassified",
      name: "Unclassified",
      color: "#6b7280",
      category: StatusCategory.NotStarted,
      orderKey: "",
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

  function handleDragOver(event: any) {
    const { active, over } = event;
    if (!over) return;

    const activeId = active.id as string;
    const overId = over.id as string;

    // Always read fresh state so we track the item through multiple dragOver events
    const currentColumns = useItemsStore.getState().columns;

    const fromColId = Object.keys(currentColumns).find((key) =>
      currentColumns[key].some((item) => item.id === activeId),
    );
    const toColId =
      statuses.some((s) => s.statusId === overId) || overId === "unclassified"
        ? overId
        : Object.keys(currentColumns).find((key) =>
            currentColumns[key].some((item) => item.id === overId),
          );

    if (!fromColId || !toColId) return;

    if (fromColId === toColId) {
      const colItems = currentColumns[fromColId] ?? [];
      const activeIndex = colItems.findIndex((item) => item.id === activeId);
      const overIndex = colItems.findIndex((item) => item.id === overId);
      if (activeIndex === -1 || overIndex === -1 || activeIndex === overIndex) return;
      useItemsStore.getState().previewMove({ activeId, fromColId, toColId, toIndex: overIndex });
    }
  }

  function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    setActiveItem(null);
    setActiveType(null);

    if (!over) return;

    const activeId = active.id as string;

    // Read the LATEST Zustand state — this reflects the final preview position after all dragOver calls
    const latestColumns = useItemsStore.getState().columns;

    const targetStatusId = Object.keys(latestColumns).find((key) =>
      latestColumns[key].some((item) => item.id === activeId),
    );
    if (!targetStatusId) return;

    const targetColItems = latestColumns[targetStatusId] ?? [];
    const targetIndex = targetColItems.findIndex((item) => item.id === activeId);

    const resolvedStatusId = targetStatusId === "unclassified" ? undefined : targetStatusId;

    // Neighbors are the items immediately around the dropped item in the final preview layout
    const stripped = targetColItems.filter((i) => i.id !== activeId);
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
        className="flex-1 flex flex-col bg-[#0a0a0b] overflow-y-auto [&::-webkit-scrollbar]:w-2 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/30 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/50 [&::-webkit-scrollbar-track]:bg-transparent scroll-smooth"
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
                onPriorityChange={onPriorityChange}
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
  onPriorityChange,
}: {
  task: TaskLayerItem;
  onClick: () => void;
  isFirst?: boolean;
  onPriorityChange?: (itemId: string, priority: Priority) => void;
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
        {onPriorityChange ? (
          <InlinePriorityPicker
            priority={task.priority as Priority}
            onPriorityChange={(p) => onPriorityChange(task.id, p)}
          />
        ) : (
          <InlinePriorityPicker
            priority={task.priority as Priority}
            onPriorityChange={() => {}}
          />
        )}
        <span className="text-xs font-medium text-[#4a4b53] tracking-wider min-w-[50px]">
          {`T-${task.id.slice(0, 4).toUpperCase()}`}
        </span>
      </div>

      <div className="flex-1 min-w-0 flex items-center gap-2">
        <div 
          className="shrink-0 h-4 w-4 flex items-center justify-center"
          style={{ color: task.color || "#FFFFFF" }}
        >
          <DynamicIcon
            name={task.icon || "Circle"}
            size={12}
            color={task.color || "#FFFFFF"}
            className="stroke-[2.5]"
          />
        </div>
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
  folder: FolderLayerItem;
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
        <DynamicIcon
          name={folder.icon || "Folder"}
          size={14}
          color={folderColor}
          className="stroke-[2.5]"
        />
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
          <DynamicIcon
            name={folder.icon || "Folder"}
            size={12}
            color={folderColor}
            className="stroke-[2.5]"
          />
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
