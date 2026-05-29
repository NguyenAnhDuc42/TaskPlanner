import * as React from "react";
import { cn } from "@/lib/utils";
import { Check, MoreHorizontal, FileText, Calendar } from "lucide-react";
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

  const itemColor = task.color || "#3b82f6";
  
  return (
    <button
      ref={setNodeRef}
      style={{ ...style, borderWidth: "1px" }}
      {...attributes}
      {...listeners}
      onClick={onSelect}
      className={cn(
        "w-full text-left flex items-start px-2.5 py-1.5 rounded-md transition-all group relative border shadow-sm outline-none",
        isSelected
          ? "bg-primary/5 border-primary/30" 
          : "bg-muted/40 border-border/50 hover:bg-muted/60"
      )}
    >
      <div
        className={cn(
          "shrink-0 mt-0.5 cursor-pointer z-10 transition-all duration-200 flex items-center justify-center",
          isChecked 
            ? "w-3.5 mr-2 opacity-100" 
            : "w-0 mr-0 opacity-0 group-hover:w-3.5 group-hover:mr-2 group-hover:opacity-100 overflow-hidden"
        )}
        onClick={(e) => {
          e.stopPropagation();
          onToggleCheck?.(task.id, e);
        }}
      >
        <div className={cn(
          "h-3.5 w-3.5 rounded-[3px] border flex items-center justify-center transition-colors shrink-0",
          isChecked
            ? "bg-primary border-primary"
            : "border-muted-foreground/30 group-hover:border-primary bg-background"
        )}>
          {isChecked && <Check className="h-2.5 w-2.5 text-primary-foreground" />}
        </div>
      </div>

      {/* Task Content Column */}
      <div className="flex flex-col gap-1 flex-1 min-w-0">

        {/* Row 1: Name + Menu */}
        <div className="flex items-center justify-between w-full gap-2">
          <div className="flex items-center gap-1.5 min-w-0">
            {task.icon ? (
              <DynamicIcon name={task.icon} className="h-3.5 w-3.5 shrink-0" color={itemColor} />
            ) : (
              <FileText className="h-3.5 w-3.5 opacity-50 shrink-0" style={{ color: itemColor }} />
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
            {task.startDate || task.dueDate ? (
              <DateBadge startDate={task.startDate} dueDate={task.dueDate} />
            ) : (
              <div className="flex items-center h-5 gap-1 px-1.5 rounded-sm bg-muted/30 text-[9px] text-muted-foreground/50 font-bold border border-border/5">
                <Calendar className="h-2.5 w-2.5 opacity-50" />
                <span>No Date</span>
              </div>
            )}
          </div>
        </div>
      </div>
    </button>
  );
}
