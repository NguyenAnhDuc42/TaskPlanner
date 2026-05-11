import { Clock, MoreHorizontal, User, Hash } from "lucide-react";
import { cn } from "@/lib/utils";
import { format } from "date-fns";
import { Priority } from "@/types/priority";
import type { TaskItemDto } from "../../layer-detail-types";

interface TaskItemProps {
  task: TaskItemDto;
  onClick: (task: TaskItemDto) => void;
  isSelected?: boolean;
}

export function TaskItem({ task, onClick, isSelected }: TaskItemProps) {
  const displayId = `T-${task.id.slice(0, 4).toUpperCase()}`;

  return (
    <div
      onClick={() => onClick(task)}
      className={cn(
        "group relative flex flex-col gap-3 p-3 rounded-md transition-all duration-300 cursor-pointer select-none active:scale-[0.98]",
        "border bg-[#0a0a0a] hover:bg-[#0f0f0f] shadow-lg",
        isSelected
          ? "border-primary/40 bg-[#121212] ring-1 ring-primary/5"
          : "border-white/[0.03] hover:border-white/[0.08] hover:shadow-primary/[0.02]"
      )}
    >
      {/* Top Metadata Row */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-1.5">
           <div className="flex items-center gap-1 px-1.5 py-0.5 rounded-md bg-white/[0.03] border border-white/[0.05] text-[8px] font-black text-muted-foreground/40 tracking-widest uppercase group-hover:text-muted-foreground/60 transition-colors">
              <Hash className="h-2 w-2" />
              {displayId}
           </div>
           
           {/* Priority Badge */}
           <div className={cn(
              "flex items-center gap-1 px-1.5 py-0.5 rounded-md border text-[8px] font-black uppercase tracking-[0.1em] transition-all duration-300",
              task.priority === Priority.Urgent ? "bg-red-500/10 border-red-500/20 text-red-400/90" :
              task.priority === Priority.High ? "bg-orange-500/10 border-orange-500/20 text-orange-400/90" :
              task.priority === Priority.Normal ? "bg-blue-500/10 border-blue-500/20 text-blue-400/90" :
              "bg-white/[0.02] border-white/[0.04] text-muted-foreground/30"
           )}>
              <div className={cn(
                 "h-1 w-1 rounded-full",
                 task.priority === Priority.Urgent ? "bg-red-500 shadow-[0_0_6px_#ef4444]" :
                 task.priority === Priority.High ? "bg-orange-500" :
                 task.priority === Priority.Normal ? "bg-blue-500" : "bg-muted-foreground/20"
              )} />
              {task.priority || "Normal"}
           </div>
        </div>
        
        <button className="p-1 opacity-0 group-hover:opacity-100 transition-all hover:bg-white/5 rounded-md">
          <MoreHorizontal className="h-3 w-3 text-muted-foreground/40" />
        </button>
      </div>

      {/* Task Name */}
      <h4 className="text-[12px] font-bold leading-relaxed text-foreground/80 group-hover:text-foreground transition-colors line-clamp-2">
        {task.name}
      </h4>

      {/* Bottom Context Row */}
      <div className="flex items-center justify-between mt-1 pt-2 border-t border-white/[0.02]">
        <div className="flex items-center gap-3">
           {task.dueDate && (
             <div className="flex items-center gap-1.5 px-2 py-1 rounded-md bg-white/[0.02] border border-white/[0.03] text-muted-foreground/40 hover:text-muted-foreground/60 transition-colors">
               <Clock className="h-2.5 w-2.5" />
               <span className="text-[9px] font-black uppercase tracking-wider">{format(new Date(task.dueDate), "MMM d")}</span>
             </div>
           )}
           
           {/* Placeholder for story points / estimate if needed */}
           {(task as any).storyPoints && (
              <div className="flex items-center gap-1 text-[9px] font-black text-primary/40">
                 <span>{(task as any).storyPoints}</span>
                 <span className="text-[7px] text-muted-foreground/20">SP</span>
              </div>
           )}
        </div>

        {/* Assignee Avatar */}
        <div className="flex -space-x-1.5">
           <div className="h-5 w-5 rounded-full bg-white/[0.03] border border-white/[0.08] flex items-center justify-center ring-2 ring-[#0a0a0a] group-hover:border-primary/30 transition-all overflow-hidden">
             <User className="h-2.5 w-2.5 text-muted-foreground/20 group-hover:text-primary/40 transition-colors" />
           </div>
        </div>
      </div>
      
      {/* Selection Glow */}
      {isSelected && (
        <div className="absolute inset-0 rounded-md bg-primary/[0.02] pointer-events-none" />
      )}
    </div>
  );
}
