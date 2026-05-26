import * as React from "react";
import { cn } from "@/lib/utils";
import { Check, MoreHorizontal, FileText } from "lucide-react";
import { StatusBadge } from "@/components/status-badge";
import { PriorityBadge } from "@/components/priority-badge";
import { DateBadge } from "@/components/date-badge";
import { DynamicIcon } from "@/components/dynamic-icon";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import type { TaskRecord } from "@/types/projects";
import type { Priority } from "@/types/priority";
import { useSelector } from "react-redux";
import { statusSelectors } from "@/store/entityStore";

export interface SortableTaskItemProps {
  task: TaskRecord;
  isSelected: boolean;
  isChecked: boolean;
  onSelect: () => void;
  onToggleCheck?: (taskId: string, e: React.MouseEvent) => void;
}

export function SortableTaskItem({ task, isSelected, isChecked, onSelect, onToggleCheck }: SortableTaskItemProps) {
  const statuses = useSelector(statusSelectors.selectAll);

  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: task.id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    zIndex: isDragging ? 10 : 1,
    opacity: isDragging ? 0.8 : 1,
  };

  return (
    <button
      ref={setNodeRef}
      style={{ ...style, borderWidth: "1px" }}
      {...attributes}
      {...listeners}
      onClick={onSelect}
      className={cn(
        "w-full text-left flex items-start gap-2.5 px-2 py-2 rounded-md transition-all group relative",
        isSelected
          ? "bg-primary/10 border-primary/20 shadow-sm"
          : "hover:bg-muted/50 border-transparent bg-background"
      )}
    >
      <div
        className="shrink-0 mt-0.5 cursor-pointer z-10"
        onClick={(e) => {
          e.stopPropagation();
          onToggleCheck?.(task.id, e);
        }}
      >
        <div className={cn(
          "h-3.5 w-3.5 rounded-[3px] border flex items-center justify-center transition-colors",
          isChecked
            ? "bg-primary border-primary"
            : "border-muted-foreground/30 group-hover:border-primary bg-background"
        )}>
          {isChecked && <Check className="h-2.5 w-2.5 text-primary-foreground" />}
        </div>
      </div>

      {/* Task Content Column */}
      <div className="flex flex-col gap-1.5 flex-1 min-w-0">

        {/* Row 1: Name + Menu */}
        <div className="flex items-center justify-between w-full gap-2">
          <div className="flex items-center gap-1.5 min-w-0">
            {task.icon ? (
              <DynamicIcon name={task.icon} className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
            ) : (
              <FileText className="h-3.5 w-3.5 text-muted-foreground opacity-50 shrink-0" />
            )}
            <span className={cn(
              "text-[12px] font-medium truncate",
              isSelected ? "text-primary font-bold" : "text-foreground"
            )}>
              {task.name}
            </span>
          </div>
          <div className="w-4 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity text-muted-foreground shrink-0 cursor-grab active:cursor-grabbing">
            <MoreHorizontal className="h-3.5 w-3.5" />
          </div>
        </div>

        {/* Row 2: Status + Priority */}
        <div className="flex items-center gap-3">
          {task.statusId && (
            <StatusBadge status={statuses.find(s => s.id === task.statusId)} />
          )}
          {task.priority && <PriorityBadge priority={task.priority as Priority} />}
        </div>

        {/* Row 3: Dates */}
        <div className="flex items-center justify-between w-full mt-0.5">
          <div className="flex items-center gap-2">
            <DateBadge startDate={task.startDate} dueDate={task.dueDate} />
          </div>
        </div>
      </div>
    </button>
  );
}
