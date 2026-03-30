import type { TaskDto } from "../../views-type";
import { Calendar, User, Flag } from "lucide-react";

interface TaskCardProps {
  task: TaskDto;
  onClick?: (task: TaskDto) => void;
}

const getPriorityVisuals = (priority: string) => {
  switch (priority?.toLowerCase()) {
    case "urgent":
      return { color: "#ef4444", label: "URGENT", glow: "rgba(239, 68, 68, 0.2)" };
    case "high":
      return { color: "#f97316", label: "HIGH", glow: "rgba(249, 115, 22, 0.2)" };
    case "normal":
      return { color: "#3b82f6", label: "NORMAL", glow: "rgba(59, 130, 246, 0.2)" };
    case "low":
      return { color: "#64748b", label: "LOW", glow: "rgba(100, 116, 139, 0.2)" };
    default:
      return { color: "#64748b", label: "NORMAL", glow: "rgba(100, 116, 139, 0.2)" };
  }
};

export function TaskCard({ task, onClick }: TaskCardProps) {
  const prio = getPriorityVisuals(task.priority);

  return (
    <div
      onClick={() => onClick?.(task)}
      className="group relative flex flex-col gap-4 p-4 rounded-3xl border border-white/5 bg-white/[0.02] hover:bg-white/[0.05] transition-all duration-300 cursor-pointer shadow-xl overflow-hidden active:scale-[0.98]"
    >
      {/* Selection Border Glow */}
      <div className="absolute inset-0 border border-primary/0 group-hover:border-primary/20 rounded-3xl transition-colors duration-300 pointer-events-none" />

      {/* Task Content */}
      <div className="flex flex-col gap-2 relative z-10">
        <div className="flex justify-between items-start gap-3">
          <span className="text-[14px] font-bold text-foreground/80 leading-snug tracking-tight group-hover:text-foreground transition-colors line-clamp-3">
            {task.name}
          </span>
        </div>
      </div>

      {/* Technical Metadata Row */}
      <div className="mt-2 flex items-center justify-between relative z-10">
        <div className="flex items-center gap-3">
          {/* Priority Indicator */}
          <div 
            className="flex items-center gap-1.5 px-2 py-0.5 rounded-md border text-[9px] font-black uppercase tracking-widest shadow-sm bg-black/20"
            style={{ 
              borderColor: `${prio.color}30`,
              color: prio.color,
              boxShadow: `0 0 10px ${prio.glow}`
            }}
          >
            <Flag className="h-2.5 w-2.5 fill-current" />
            {prio.label}
          </div>

          <div className="h-6 w-6 rounded-full bg-white/5 border border-white/10 flex items-center justify-center shadow-inner group-hover:border-white/20 transition-all">
            <User className="h-3 w-3 text-muted-foreground/30 group-hover:text-muted-foreground/60 transition-colors" />
          </div>
        </div>

        {task.dueDate && (
          <div className="flex items-center gap-1.5 text-[9px] font-black text-muted-foreground/30 uppercase tracking-[0.15em] bg-white/5 px-2 py-1 rounded-lg group-hover:text-muted-foreground/60 transition-colors">
            <Calendar className="h-3 w-3 opacity-40" />
            {new Date(task.dueDate).toLocaleDateString(undefined, {
              month: "short",
              day: "numeric",
            })}
          </div>
        )}
      </div>
    </div>
  );
}
