import React from "react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { Priority } from "@/types/priority";
import type { BoardItem } from "../space-api";
import { cn } from "@/lib/utils";
import { format, isBefore, startOfDay } from "date-fns";
import { PrioritySelect } from "@/components/priority-select";
import { PriorityBadge } from "@/components/priority-badge";
import { DynamicIcon } from "@/components/dynamic-icon";
import { Maximize2, User } from "lucide-react";
import { DateSelect } from "@/components/date-select";
import type { FolderRecord, TaskRecord } from "@/types/projects";
import { useRouter, useNavigate } from "@tanstack/react-router";
import { useWorkspace } from "@/features/workspace/context/workspace-context";

export interface BoardItemCardProps {
  item: BoardItem;
  isSelected?: boolean;
  onClick?: () => void;
  onDoubleClick?: () => void;
  onMouseDown?: () => void;
  onMaximizeClick?: () => void;
  onPriorityChange?: (priority: Priority) => void;
  onDateChange?: (patches: { startDate?: string; dueDate?: string; clearStartDate?: boolean; clearDueDate?: boolean }) => void;
  isDragging?: boolean;
  style?: React.CSSProperties;
  dragRef?: (node: HTMLElement | null) => void;
  dragProps?: Record<string, unknown>;
}

export const BoardItemCard = React.memo(function BoardItemCard({
  item,
  isSelected,
  onClick,
  onDoubleClick,
  onMouseDown,
  onMaximizeClick,
  onPriorityChange,
  onDateChange,
  isDragging,
  style,
  dragRef,
  dragProps,
}: BoardItemCardProps) {
  const itemColor = item.color || (item.__type === "folder" ? "#3b82f6" : "#6b7280");
  const isOverdue = React.useMemo(() => {
    return item.dueDate ? isBefore(startOfDay(new Date(item.dueDate)), startOfDay(new Date())) : false;
  }, [item.dueDate]);

  const dateInfo = React.useMemo(() => {
    const show = !!item.startDate || !!item.dueDate;
    let text = "";
    if (show) {
      if (item.startDate && item.dueDate) {
        text = `${format(new Date(item.startDate), "MMM d")} - ${format(new Date(item.dueDate), "MMM d")}`;
      } else if (item.startDate) {
        text = `Start: ${format(new Date(item.startDate), "MMM d")}`;
      } else if (item.dueDate) {
        text = isOverdue 
          ? `Overdue: ${format(new Date(item.dueDate), "MMM d")}` 
          : `Due: ${format(new Date(item.dueDate), "MMM d")}`;
      }
    }
    const createdText = item.createdAt ? format(new Date(item.createdAt), "MMM d") : "";
    return { show, text, createdText };
  }, [item.startDate, item.dueDate, item.createdAt, isOverdue]);

  return (
    <div
      ref={dragRef}
      {...dragProps}
      onClick={onClick}
      onDoubleClick={onDoubleClick}
      onMouseDown={onMouseDown}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (onClick && (e.key === "Enter" || e.key === " ")) {
          e.preventDefault();
          onClick();
        }
      }}
      className={cn(
        "group relative flex flex-col gap-1.5 p-2 rounded-lg cursor-grab active:cursor-grabbing select-none border outline-none shadow-sm shrink-0 w-full text-card-foreground",
        isSelected
          ? "bg-primary/5 border-primary/30"
          : "bg-muted/40 border-border/50 hover:bg-muted/60",
        isDragging && "opacity-0 pointer-events-none border-transparent bg-transparent shadow-none"
      )}
      style={style}
    >
      <div className="flex flex-col gap-1.5 w-full h-full">
        {/* Row 1: Icon, Name (Left) | Expand, MoreVertical (Right) */}
        <div className="flex items-center justify-between mt-0.5">
          <div className="flex items-center gap-2 min-w-0 flex-1">
            <div className="shrink-0 h-4.5 w-4.5 flex items-center justify-center" style={{ color: itemColor }}>
              <DynamicIcon
                name={item.icon || (item.__type === "folder" ? "Folder" : "Circle")}
                size={13}
                color={itemColor}
                className="stroke-[2.5]"
              />
            </div>
            <h4 className={cn(
              "text-[12px] font-medium leading-tight transition-colors truncate w-full pr-2",
              isSelected ? "text-primary font-bold" : "text-zinc-300 group-hover:text-white"
            )}>
              {item.name}
            </h4>
          </div>
          <div className="flex items-center shrink-0 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity">
            <button 
              type="button" 
              className="p-0.5 hover:bg-white/10 rounded-sm hover:text-white transition-colors" 
              onClick={(e) => { 
                e.stopPropagation(); 
                onMaximizeClick?.();
              }}
            >
              <Maximize2 className="h-3 w-3" />
            </button>
          </div>
        </div>

        {/* Row 2: Priority, Date (Left) | Created (Right) */}
        <div className="flex items-center justify-between text-[10px] font-medium leading-none mt-1 w-full gap-2">
          <div className="flex items-center gap-1.5 flex-wrap">
            <PrioritySelect
              value={(item as TaskRecord | FolderRecord).priority as Priority}
              onChange={(p) => onPriorityChange?.(p)}
              align="start"
              trigger={
                <button 
                  type="button" 
                  className="cursor-pointer focus:outline-none bg-transparent border-none p-0 hover:opacity-80 transition-opacity"
                  onClick={(e) => e.stopPropagation()}
                  onPointerDown={(e) => e.stopPropagation()}
                >
                  <PriorityBadge priority={(item as TaskRecord | FolderRecord).priority as Priority} />
                </button>
              }
            />

            <DateSelect
              startDate={item.startDate}
              dueDate={item.dueDate}
              onStartDateChange={(date) => onDateChange?.(date ? { startDate: date.toISOString() } : { clearStartDate: true })}
              onDueDateChange={(date) => onDateChange?.(date ? { dueDate: date.toISOString() } : { clearDueDate: true })}
              onClearDates={() => onDateChange?.({ clearStartDate: true, clearDueDate: true })}
              size="sm"
              align="start"
              triggerClassName={cn(
                "h-5 px-1.5 text-[8px] font-black uppercase tracking-wider border border-border/5 rounded w-fit leading-none",
                isOverdue 
                  ? "bg-red-500/10 text-red-400 border-red-500/20 hover:bg-red-500/20" 
                  : "bg-white/[0.02] text-zinc-500 border-white/[0.04] hover:bg-white/[0.06]"
              )}
            />
          </div>
        </div>

        {/* Row 3: Assignee Mock (Left) | Folder Tag & Created (Right) */}
        <div className="flex items-center justify-between text-[10px] font-medium leading-none mt-1 w-full gap-2">
          <div className="flex items-center gap-2">
            <div className="h-4 w-4 rounded-full bg-white/[0.03] flex items-center justify-center border border-white/[0.05]">
              <User className="h-2.5 w-2.5 opacity-30 group-hover:opacity-50 transition-opacity" />
            </div>
          </div>
          
          <div className="flex items-center gap-1.5 ml-auto">
            {item.__type === "folder" && (
              <span 
                className="px-1.5 py-0.5 rounded text-[8px] uppercase font-black tracking-widest border shrink-0"
                style={{
                  backgroundColor: `${itemColor}0e`,
                  color: itemColor,
                  borderColor: `${itemColor}22`
                }}
              >
                {item.__type}
              </span>
            )}
            {dateInfo.createdText && (
              <span className="text-[9px] text-muted-foreground/45 font-medium shrink-0 select-none">
                {dateInfo.createdText}
              </span>
            )}
          </div>
        </div>
      </div>
    </div>
  );
});

export const SortableBoardItem = React.memo(function SortableBoardItem({
  item,
  isSelected,
  onTaskClick,
  onFolderClick,
  onPriorityChange,
  onDateChange,
}: {
  item: BoardItem;
  isSelected?: boolean;
  onTaskClick: (id: string) => void;
  onFolderClick: (id: string) => void;
  onPriorityChange: (id: string, type: "task" | "folder", priority: Priority) => void;
  onDateChange: (id: string, type: "task" | "folder", patches: { startDate?: string; dueDate?: string; clearStartDate?: boolean; clearDueDate?: boolean }) => void;
}) {
  const { canCreateContent } = useWorkspaceRole();
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({
    id: `${item.__type}-${item.id}`,
  });

  const { workspaceId } = useWorkspace();
  const navigate = useNavigate();
  const router = useRouter();

  const style = React.useMemo(() => ({
    transform: isDragging ? undefined : CSS.Transform.toString(transform),
    transition: isDragging ? undefined : transition,
  }), [isDragging, transform, transition]);

  
  const handleClick = React.useCallback(() => {
    if (item.__type === "task") {
      onTaskClick(item.id);
    } else {
      onFolderClick(item.id);
    }
  }, [item.id, item.__type, onTaskClick, onFolderClick]);

  const handleOpenPage = React.useCallback(() => {
    if (!workspaceId) return;
    if (item.__type === "task") {
      navigate({
        to: "/workspaces/$workspaceId/tasks/$taskId",
        params: { workspaceId, taskId: item.id },
      });
    } else {
      navigate({
        to: "/workspaces/$workspaceId/folders/$folderId",
        params: { workspaceId, folderId: item.id },
      });
    }
  }, [item.id, item.__type, workspaceId, navigate]);

  const handleMouseDown = React.useCallback(() => {
    if (!workspaceId) return;
    if (item.__type === "task") {
      router.preloadRoute({
        to: "/workspaces/$workspaceId/tasks/$taskId",
        params: { workspaceId, taskId: item.id },
      });
    } else {
      router.preloadRoute({
        to: "/workspaces/$workspaceId/folders/$folderId",
        params: { workspaceId, folderId: item.id },
      });
    }
  }, [item.id, item.__type, workspaceId, router]);

  const handlePrioritySelect = React.useCallback((p: Priority) => {
    onPriorityChange(item.id, item.__type, p);
  }, [item.id, item.__type, onPriorityChange]);

  const handleDateSelect = React.useCallback((patches: { startDate?: string; dueDate?: string; clearStartDate?: boolean; clearDueDate?: boolean }) => {
    onDateChange(item.id, item.__type, patches);
  }, [item.id, item.__type, onDateChange]);

  return (
    <BoardItemCard
      item={item}
      isSelected={isSelected}
      onClick={handleClick}
      onDoubleClick={handleOpenPage}
      onMouseDown={handleMouseDown}
      onMaximizeClick={handleOpenPage}
      onPriorityChange={handlePrioritySelect}
      onDateChange={handleDateSelect}
      isDragging={isDragging}
      style={style}
      dragRef={setNodeRef}
      dragProps={{ ...attributes, ...(canCreateContent ? listeners : {}) }}
    />
  );
});
