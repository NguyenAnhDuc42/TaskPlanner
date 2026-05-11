import { Circle, Package, User } from "lucide-react";
import { cn } from "@/lib/utils";
import { format } from "date-fns";
import { PriorityBadge } from "@/components/priority-badge";
import type { TaskItemDto } from "../../layer-detail-types";
import { Priority } from "@/types/priority";

interface TaskItemProps {
  task: TaskItemDto;
  onClick: (task: TaskItemDto) => void;
  isSelected?: boolean;
}

export function TaskItem({ task, onClick, isSelected }: TaskItemProps) {
  return (
    <div
      onClick={() => onClick(task)}
      className={cn(
        "group relative flex flex-col gap-2 p-3 rounded-lg transition-all duration-200 cursor-pointer select-none",
        "border bg-[#0d0d0e]/80 hover:bg-[#161618] border-border/20 hover:border-border/40",
        isSelected && "border-primary/40 bg-[#121212]"
      )}
    >
      {/* 1. ID and Avatar Row */}
      <div className="flex items-center justify-between text-[11px] text-muted-foreground/40 font-medium">
        <span>{`SOM-${task.id.slice(0, 4).toUpperCase()}`}</span>
        <div className="flex items-center gap-2">
          {task.priority && <PriorityBadge priority={task.priority as Priority} />}
          <div className="h-4 w-4 rounded-full bg-white/5 flex items-center justify-center border border-white/10">
            <User className="h-2.5 w-2.5 opacity-40" />
          </div>
        </div>
      </div>

      {/* 2. Status & Title Row */}
      <div className="flex items-center gap-2">
        <Circle className="h-3 w-3 text-muted-foreground/40 shrink-0" />
        <h4 className="text-[12px] font-medium leading-tight text-foreground/90 group-hover:text-foreground transition-colors truncate">
          {task.name}
        </h4>
      </div>

      {/* 3. Placeholders Row (Three dots & Box) */}
      <div className="flex items-center gap-1.5 mt-0.5">
        <div className="text-muted-foreground/30 text-[12px]">...</div>
        <div className="flex items-center gap-1 px-1.5 py-0.5 rounded bg-white/5 border border-white/5 text-muted-foreground/40 text-[10px]">
          <Package className="h-2.5 w-2.5" />
          <span>1</span>
        </div>
      </div>

      {/* 4. Date Row */}
      <div className="text-[10px] text-muted-foreground/30 mt-0.5">
        {`Created ${format(new Date(task.createdAt), "MMMM d")}`}
      </div>
    </div>
  );
}
