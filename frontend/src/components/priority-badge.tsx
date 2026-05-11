import { cn } from "@/lib/utils";
import { Priority } from "@/types/priority";

interface PriorityBadgeProps {
  priority?: Priority;
}

export function PriorityBadge({ priority }: PriorityBadgeProps) {
  return (
    <div className={cn(
      "flex items-center gap-1 px-1.5 py-0.5 rounded-md border text-[8px] font-black uppercase tracking-[0.1em] transition-all duration-300",
      priority === Priority.Urgent ? "bg-red-500/10 border-red-500/20 text-red-400/90" :
      priority === Priority.High ? "bg-orange-500/10 border-orange-500/20 text-orange-400/90" :
      priority === Priority.Normal ? "bg-blue-500/10 border-blue-500/20 text-blue-400/90" :
      "bg-white/[0.02] border-white/[0.04] text-muted-foreground/30"
    )}>
      <div className={cn(
        "h-1 w-1 rounded-full",
        priority === Priority.Urgent ? "bg-red-500 shadow-[0_0_6px_#ef4444]" :
        priority === Priority.High ? "bg-orange-500" :
        priority === Priority.Normal ? "bg-blue-500" : "bg-muted-foreground/20"
      )} />
      {priority || "Normal"}
    </div>
  );
}
