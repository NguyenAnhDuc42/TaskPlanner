import { User } from "lucide-react";
import { cn } from "@/lib/utils";
import { format } from "date-fns";
import type { TaskItemDto } from "../../layer-detail-types";
import { Priority } from "@/types/priority";
import { DynamicIcon } from "@/components/dynamic-icon";
import { InlinePriorityPicker } from "./inline-priority-picker";

interface TaskItemProps {
  task: TaskItemDto;
  onClick: (task: TaskItemDto) => void;
  isSelected?: boolean;
  onPriorityChange?: (itemId: string, priority: Priority) => void;
}

export function TaskItem({ task, onClick, isSelected, onPriorityChange }: TaskItemProps) {
  return (
    <div
      onClick={() => onClick(task)}
      className={cn(
        "group relative flex flex-col gap-2 p-3 rounded-lg transition-all duration-200 cursor-pointer select-none",
        "border bg-[#0d0d0e]/80 hover:bg-[#161618] border-border/20 hover:border-border/40",
        isSelected && "border-primary/40 bg-[#121212]"
      )}
    >
      {/* 1. Type Marker and Avatar Row */}
      <div className="flex items-center justify-between text-[11px] text-muted-foreground/40 font-medium">
        <span className="px-1.5 py-0.5 rounded bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 text-[8px] uppercase font-black tracking-widest leading-none">Task</span>
        <div className="flex items-center gap-2">
          <div className="h-4 w-4 rounded-full bg-white/5 flex items-center justify-center border border-white/10">
            <User className="h-2.5 w-2.5 opacity-40" />
          </div>
        </div>
      </div>

      {/* 2. Status & Title Row */}
      <div className="flex items-center gap-2">
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
        <h4 className="text-[12px] font-medium leading-tight text-foreground/90 group-hover:text-foreground transition-colors truncate">
          {task.name}
        </h4>
      </div>

      {/* 3. Priority Badge under Title */}
      <div className="flex items-center gap-1.5 mt-1 min-h-[18px]">
        {onPriorityChange ? (
          <InlinePriorityPicker
            priority={task.priority as Priority}
            onPriorityChange={(p) => onPriorityChange(task.id, p)}
          />
        ) : (
          task.priority && (
            <InlinePriorityPicker
              priority={task.priority as Priority}
              onPriorityChange={() => {}}
            />
          )
        )}
      </div>

      {/* 4. Date Row */}
      <div className="text-[10px] text-muted-foreground/30 mt-0.5">
        {`Created ${format(new Date(task.createdAt), "MMMM d")}`}
      </div>
    </div>
  );
}
