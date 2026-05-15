import { cn } from "@/lib/utils";
import { type Status } from "@/types/status";

interface StatusBadgeProps {
  status?: Status | null;
  className?: string;
  showIcon?: boolean;
}

export function StatusBadge({ status, className, showIcon = true }: StatusBadgeProps) {
  if (!status) {
    return (
      <div className={cn("inline-flex items-center gap-1.5 px-2 py-1 rounded-md bg-muted/10 border border-border/5", className)}>
        <div className="h-2 w-2 rounded-[2px] bg-muted-foreground/20" />
        <span className="text-[9px] font-black text-muted-foreground/40 uppercase tracking-widest">
          No Status
        </span>
      </div>
    );
  }

  const statusColor = status.color || "#888";

  return (
    <div 
      className={cn("inline-flex items-center gap-2 px-2 py-1 rounded-md transition-all duration-300", className)}
      style={{ 
        backgroundColor: `${statusColor}15`,
        border: `1px solid ${statusColor}15`
      }}
    >
      {showIcon && (
        <div 
          className="h-2 w-2 rounded-[2px] shadow-sm transition-transform group-hover:scale-110" 
          style={{ backgroundColor: statusColor }} 
        />
      )}
      <span 
        className="text-[9px] font-black tracking-widest uppercase"
        style={{ color: statusColor }}
      >
        {status.name}
      </span>
    </div>
  );
}
