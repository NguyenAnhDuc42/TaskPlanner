import React from "react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { Priority } from "@/types/priority";
import type { BoardItem } from "../space-board-types";
import { cn } from "@/lib/utils";
import { format, isBefore, startOfDay } from "date-fns";
import { PrioritySelect } from "@/components/priority-select";
import { PriorityBadge } from "@/components/priority-badge";
import { DynamicIcon } from "@/components/dynamic-icon";
import { Maximize2, MoreVertical } from "lucide-react";
import { DateSelect } from "@/components/date-select";
import type { TaskRecord } from "@/types/projects";
import { useRouter, useNavigate } from "@tanstack/react-router";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { TaskContextMenu } from "@/features/workspace/contents/hierarchy/hierarchy-components/context-menus/task-context-menu";
import { EntityMenuTrigger } from "@/features/workspace/contents/hierarchy/hierarchy-components/context-menus/shared";

export interface BoardItemCardProps {
  item: BoardItem;
  isSelected?: boolean;
  onClick?: () => void;
  onDoubleClick?: () => void;
  onMouseDown?: () => void;
  onMaximizeClick?: () => void;
  onPriorityChange?: (priority: Priority) => void;
  onDateChange?: (patches: { startDate?: string | null; dueDate?: string | null }) => void;
  isDragging?: boolean;
  style?: React.CSSProperties;
  dragRef?: (node: HTMLElement | null) => void;
  dragProps?: Record<string, unknown>;
  canCreateContent?: boolean;
  // False for the floating DragOverlay preview — it's not wrapped in a TaskContextMenu, so the
  // kebab trigger (which needs that context) can't render there.
  showActions?: boolean;
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
  canCreateContent,
  showActions = true,
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
          ? "bg-primary/5 border-primary/50"
          : "bg-card border-border/60 hover:border-border  hover:shadow-md hover:bg-card",
        isDragging && "opacity-0 pointer-events-none border-transparent bg-transparent shadow-none"
      )}
      style={style}
    >
      <div className="flex flex-col gap-1.5 w-full h-full">
        {/* Row 1: Icon, Name | Expand */}
        <div className="flex flex-col gap-0.5 mt-0.5">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2 min-w-0 flex-1">
              <div className="shrink-0" style={{ color: itemColor }}>
                <DynamicIcon name={item.icon || "Circle"} size={13} color={itemColor} className="stroke-[2.5]" />
              </div>
              <h4 className={cn(
                "text-[12px] font-medium leading-tight truncate pr-2",
                isSelected ? "text-primary font-bold" : "text-foreground"
              )}>
                {item.name}
              </h4>
            </div>
            {showActions && (
              <div className="flex items-center gap-0.5 shrink-0 text-muted-foreground opacity-100 md:opacity-0 md:group-hover:opacity-100 transition-opacity">
                {/* Hover-only on desktop (revealed via group-hover); always visible below md so
                    touch users — who have no hover state — can still reach these actions. */}
                <EntityMenuTrigger>
                  <button
                    type="button"
                    className="p-0.5 hover:bg-muted/50 rounded-md hover:text-foreground transition-colors"
                    onClick={(e) => e.stopPropagation()}
                    onPointerDown={(e) => e.stopPropagation()}
                  >
                    <MoreVertical className="h-3 w-3" />
                  </button>
                </EntityMenuTrigger>
                <button
                  type="button"
                  className="p-0.5 hover:bg-muted/50 rounded-md hover:text-foreground transition-colors"
                  onClick={(e) => { e.stopPropagation(); onMaximizeClick?.(); }}
                >
                  <Maximize2 className="h-3 w-3" />
                </button>
              </div>
            )}
          </div>
        </div>

        {/* Row 2: Priority, Date — flat/minimal (icon + plain text), matching the mock rather
            than the old boxed-badge treatment. Always visible, not gated on selection. */}
        <div className="flex items-center text-[9px] font-semibold leading-none mt-1 w-full gap-2">
          <div className="flex items-center gap-2 flex-wrap">
            {canCreateContent ? (
              <PrioritySelect
                value={(item as TaskRecord).priority as Priority}
                onChange={(p) => onPriorityChange?.(p)}
                align="start"
                trigger={
                  <button
                    type="button"
                    className="cursor-pointer focus:outline-none bg-transparent border-none p-0 hover:opacity-80 transition-opacity flex items-center"
                    onClick={(e) => e.stopPropagation()}
                    onPointerDown={(e) => e.stopPropagation()}
                  >
                    <PriorityBadge priority={(item as TaskRecord).priority as Priority} showText={false} className="h-auto! p-0! bg-transparent!" />
                  </button>
                }
              />
            ) : (
              <PriorityBadge priority={(item as TaskRecord).priority as Priority} showText={false} className="h-auto! p-0! bg-transparent!" />
            )}

            {canCreateContent ? (
              <DateSelect
                startDate={item.startDate}
                dueDate={item.dueDate}
                onStartDateChange={(date) => onDateChange?.({ startDate: date ? date.toISOString() : null })}
                onDueDateChange={(date) => onDateChange?.({ dueDate: date ? date.toISOString() : null })}
                onClearDates={() => onDateChange?.({ startDate: null, dueDate: null })}
                size="sm"
                align="start"
                triggerClassName={cn(
                  "h-auto p-0 bg-transparent border-none text-[9px] font-semibold leading-none w-fit",
                  isOverdue ? "text-destructive" : "text-muted-foreground/50"
                )}
              />
            ) : dateInfo.show ? (
              <span className={cn("text-[9px] font-semibold leading-none", isOverdue ? "text-destructive" : "text-muted-foreground/50")}>
                {dateInfo.text}
              </span>
            ) : null}

            {dateInfo.createdText && (
              <span className="text-[9px] text-muted-foreground/45 font-medium shrink-0 select-none ml-auto">
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
  onPriorityChange,
  onDateChange,
}: {
  item: BoardItem;
  isSelected?: boolean;
  onTaskClick: (id: string) => void;
  onPriorityChange: (id: string, priority: Priority) => void;
  onDateChange: (id: string, patches: { startDate?: string | null; dueDate?: string | null }) => void;
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

  const handleMouseDown = React.useCallback(() => {
    if (!workspaceId) return;
    router.preloadRoute({ to: "/workspaces/$workspaceId/tasks/$taskId", params: { workspaceId, taskId: item.id } });
  }, [item.id, workspaceId, router]);

  const handlePrioritySelect = React.useCallback((p: Priority) => {
    onPriorityChange(item.id, p);
  }, [item.id, onPriorityChange]);

  const handleDateSelect = React.useCallback((patches: { startDate?: string | null; dueDate?: string | null }) => {
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
