import { Circle } from "lucide-react";
import { cn } from "@/lib/utils";
import type { TaskItemDto } from "../../views-type";

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
        "group flex items-center gap-4 p-3 rounded-xl transition-all cursor-pointer active:scale-[0.99]",
        isSelected
          ? "bg-primary/5 border border-primary/10 shadow-sm"
          : "hover:bg-muted/50 border border-transparent"
      )}
    >
      <div className="p-1.5 rounded-lg text-muted-foreground/30 group-hover:text-primary transition-colors">
        <Circle className="h-3 w-3" />
      </div>
      <span className="text-[14px] font-bold text-foreground/70 group-hover:text-foreground transition-colors">
        {task.name}
      </span>
      <div className="ml-auto flex items-center gap-4">
        <span className="text-[9px] font-black text-muted-foreground/20 uppercase tracking-[0.2em] opacity-0 group-hover:opacity-100 transition-opacity">
          {task.priority || "Normal"}
        </span>
      </div>
    </div>
  );
}
