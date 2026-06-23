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
import type { TaskRecord } from "@/types/projects";
import { useRouter, useNavigate } from "@tanstack/react-router";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { TaskContextMenu } from "@/features/workspace/contents/hierarchy/hierarchy-components/context-menus/task-context-menu";

export interface BoardItemCardProps {
  item: BoardItem;
  isSelected?: boolean;
  onClick?: () => void;
  onDoubleClick?: () => void;
  onMouseDown?: () => void;
  onMaximizeClick?: () => void;
  onFolderClick?: () => void;
  onPriorityChange?: (priority: Priority) => void;
  onDateChange?: (patches: { startDate?: string; dueDate?: string; clearStartDate?: boolean; clearDueDate?: boolean }) => void;
  isDragging?: boolean;
  style?: React.CSSProperties;
  dragRef?: (node: HTMLElement | null) => void;
  dragProps?: Record<string, unknown>;
  canCreateContent?: boolean;
}

export const BoardItemCard = React.memo(function BoardItemCard({
  item,
  isSelected,
  onClick,
  onDoubleClick,
  onMouseDown,
  onMaximizeClick,
  onFolderClick,
  onPriorityChange,
  onDateChange,
  isDragging,
  style,
  dragRef,
  dragProps,
  canCreateContent,
}: BoardItemCardProps) {
  const itemColor = item.color || "#6b7280";
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
        {/* Row 1: Icon, Name | Expand */}
        <div className="flex items-center justify-between mt-0.5">
          <div className="flex items-center gap-2 min-w-0 flex-1">
            <div className="shrink-0" style={{ color: itemColor }}>
              <DynamicIcon name={item.icon || "Circle"} size={13} color={itemColor} className="stroke-[2.5]" />
            </div>
            <h4 className={cn(
              "text-[12px] font-medium leading-tight transition-colors truncate w-full pr-2",
              isSelected ? "text-primary font-bold" : "text-zinc-300 group-hover:text-white"
            )}>
              {item.folderName ? (
                <>
                  <span
                    className="text-muted-foreground/50 font-normal hover:text-primary/70 transition-colors cursor-pointer"
                    onClick={(e) => { e.stopPropagation(); onFolderClick?.(); }}
                    onPointerDown={(e) => e.stopPropagation()}
                  >
                    {item.folderName.length > 12 ? `${item.folderName.slice(0, 12)}…` : item.folderName}
                    {" › "}
                  </span>
                  {item.name}
                </>
              ) : item.name}
            </h4>
          </div>
          <div className="flex items-center shrink-0 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity">
            <button
              type="button"
              className="p-0.5 hover:bg-white/10 rounded-sm hover:text-white transition-colors"
              onClick={(e) => { e.stopPropagation(); onMaximizeClick?.(); }}
            >
              <Maximize2 className="h-3 w-3" />
            </button>
          </div>
        </div>

        {/* Row 2: Priority, Date */}
        <div className="flex items-center text-[10px] font-medium leading-none mt-1 w-full gap-2">
          <div className="flex items-center gap-1.5 flex-wrap">
            {canCreateContent ? (
              <PrioritySelect
                value={(item as TaskRecord).priority as Priority}
                onChange={(p) => onPriorityChange?.(p)}
                align="start"
                trigger={
                  <button
                    type="button"
                    className="cursor-pointer focus:outline-none bg-transparent border-none p-0 hover:opacity-80 transition-opacity"
                    onClick={(e) => e.stopPropagation()}
                    onPointerDown={(e) => e.stopPropagation()}
                  >
                    <PriorityBadge priority={(item as TaskRecord).priority as Priority} />
                  </button>
                }
              />
            ) : (
              <PriorityBadge priority={(item as TaskRecord).priority as Priority} />
            )}

            {canCreateContent ? (
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
                    : "bg-white/2 text-zinc-500 border-white/4 hover:bg-white/[0.06]"
                )}
              />
            ) : dateInfo.show ? (
              <span className={cn(
                "h-5 px-1.5 text-[8px] font-black uppercase tracking-wider border border-border/5 rounded w-fit leading-none flex items-center",
                isOverdue
                  ? "bg-red-500/10 text-red-400 border-red-500/20"
                  : "bg-white/2 text-zinc-500 border-white/4"
              )}>
                {dateInfo.text}
              </span>
            ) : null}
          </div>
        </div>

        {/* Row 3: Assignee | Created */}
        <div className="flex items-center justify-between text-[10px] font-medium leading-none mt-1 w-full gap-2">
          <div className="h-4 w-4 rounded-full bg-white/3 flex items-center justify-center border border-white/5">
            <User className="h-2.5 w-2.5 opacity-30 group-hover:opacity-50 transition-opacity" />
          </div>
          {dateInfo.createdText && (
            <span className="text-[9px] text-muted-foreground/45 font-medium shrink-0 select-none ml-auto">
              {dateInfo.createdText}
            </span>
          )}
        </div>
      </div>
    </div>
  );
});

export const SortableBoardItem = React.memo(function SortableBoardItem({
  item,
  isSelected,
  onTaskClick,
  onPriorityChange,
  onDateChange,
}: {
  item: BoardItem;
  isSelected?: boolean;
  onTaskClick: (id: string) => void;
  onPriorityChange: (id: string, priority: Priority) => void;
  onDateChange: (id: string, patches: { startDate?: string; dueDate?: string; clearStartDate?: boolean; clearDueDate?: boolean }) => void;
}) {
  const { canCreateContent } = useWorkspaceRole();
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: `task-${item.id}`,
  });

  const { workspaceId } = useWorkspace();
  const navigate = useNavigate();
  const router = useRouter();

  const style = React.useMemo(() => ({
    transform: isDragging ? undefined : CSS.Transform.toString(transform),
    transition: isDragging ? undefined : transition,
  }), [isDragging, transform, transition]);

  const handleClick = React.useCallback(() => {
    onTaskClick(item.id);
  }, [item.id, onTaskClick]);

  const handleOpenPage = React.useCallback(() => {
    if (!workspaceId) return;
    navigate({ to: "/workspaces/$workspaceId/tasks/$taskId", params: { workspaceId, taskId: item.id } });
  }, [item.id, workspaceId, navigate]);

  const handleFolderClick = React.useCallback(() => {
    if (!workspaceId || !item.folderId) return;
    navigate({ to: "/workspaces/$workspaceId/folders/$folderId", params: { workspaceId, folderId: item.folderId } });
  }, [item.folderId, workspaceId, navigate]);

  const handleMouseDown = React.useCallback(() => {
    if (!workspaceId) return;
    router.preloadRoute({ to: "/workspaces/$workspaceId/tasks/$taskId", params: { workspaceId, taskId: item.id } });
  }, [item.id, workspaceId, router]);

  const handlePrioritySelect = React.useCallback((p: Priority) => {
    onPriorityChange(item.id, p);
  }, [item.id, onPriorityChange]);

  const handleDateSelect = React.useCallback((patches: { startDate?: string; dueDate?: string; clearStartDate?: boolean; clearDueDate?: boolean }) => {
    onDateChange(item.id, patches);
  }, [item.id, onDateChange]);

  return (
    <TaskContextMenu taskId={item.id} taskName={item.name} parentId="">
      <div>
        <BoardItemCard
          item={item}
          isSelected={isSelected}
          onClick={handleClick}
          onDoubleClick={handleOpenPage}
          onMouseDown={handleMouseDown}
          onMaximizeClick={handleOpenPage}
          onFolderClick={item.folderId ? handleFolderClick : undefined}
          onPriorityChange={handlePrioritySelect}
          onDateChange={handleDateSelect}
          isDragging={isDragging}
          style={style}
          dragRef={setNodeRef}
          dragProps={{ ...attributes, ...(canCreateContent ? listeners : {}) }}
          canCreateContent={canCreateContent}
        />
      </div>
    </TaskContextMenu>
  );
});
