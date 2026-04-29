import { Clock, MoreHorizontal, User } from "lucide-react";
import { cn } from "@/lib/utils";
import type { TaskItemDto } from "../../views-type";
import { format } from "date-fns";
import { Priority } from "@/types/priority";

interface TaskItemProps {
  task: TaskItemDto;
  onClick: (task: TaskItemDto) => void;
  isSelected?: boolean;
}

export function TaskItem({ task, onClick, isSelected }: TaskItemProps) {
  // Mock ID for visual completeness (Linear style)
  const displayId = `TASK-${task.id.slice(0, 4).toUpperCase()}`;

  return (
    <div
      onClick={() => onClick(task)}
      className={cn(
        "group flex flex-col gap-2 p-2.5 rounded-md transition-all cursor-pointer select-none active:scale-[0.98]",
        "border bg-[#0c0c0c] hover:bg-[#111111] shadow-sm",
        isSelected
          ? "border-primary/50 bg-[#141414] ring-1 ring-primary/10"
          : "border-white/[0.04] hover:border-white/[0.08]"
      )}
    >
      {/* Top row: ID and metadata icons */}
      <div className="flex items-center justify-between">
        <span className="text-[9px] font-black text-muted-foreground/30 tracking-wider group-hover:text-muted-foreground/50 transition-colors">
          {displayId}
        </span>
        <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
          <MoreHorizontal className="h-3 w-3 text-muted-foreground/30 hover:text-foreground" />
        </div>
      </div>

      {/* Title */}
      <span className="text-[12px] font-bold leading-tight text-foreground/80 group-hover:text-foreground transition-colors line-clamp-2">
        {task.name}
      </span>

      {/* Bottom row: Status and metadata */}
      <div className="flex items-center gap-2 mt-0.5">
        {/* Priority Icon (Linear style) */}
        <div className={cn(
          "flex items-center gap-1.5 px-1 py-0.5 rounded-sm border transition-colors",
          task.priority === Priority.Urgent ? "bg-red-500/5 border-red-500/10 text-red-400/80" :
          task.priority === Priority.High ? "bg-orange-500/5 border-orange-500/10 text-orange-400/80" :
          task.priority === Priority.Normal ? "bg-blue-500/5 border-blue-500/10 text-blue-400/80" :
          "bg-white/[0.02] border-white/[0.04] text-muted-foreground/40"
        )}>
          <div className={cn(
            "h-1 w-1 rounded-full",
            task.priority === Priority.Urgent ? "bg-red-500 shadow-[0_0_4px_rgba(239,68,68,0.4)]" :
            task.priority === Priority.High ? "bg-orange-500" :
            task.priority === Priority.Normal ? "bg-blue-500" : "bg-muted-foreground/30"
          )} />
          <span className="text-[8px] font-black uppercase tracking-widest">
            {task.priority?.charAt(0) || "L"}
          </span>
        </div>

        {/* Date if exists */}
        {task.dueDate && (
          <div className="flex items-center gap-1 text-muted-foreground/40">
            <Clock className="h-2.5 w-2.5" />
            <span className="text-[9px] font-bold">{format(new Date(task.dueDate), "MMM d")}</span>
          </div>
        )}

        {/* User Placeholder */}
        <div className="ml-auto">
          <div className="h-4.5 w-4.5 rounded-full bg-white/[0.02] border border-white/[0.05] flex items-center justify-center group-hover:border-primary/20 transition-colors">
            <User className="h-2 w-2 text-muted-foreground/30 group-hover:text-primary/50 transition-colors" />
          </div>
        </div>
      </div>
    </div>
  );
}
