import { cn } from "@/lib/utils";
import { type StatusDto } from "@/types/status";

interface StatusBadgeProps {
  status?: StatusDto | null;
  className?: string;
  showDot?: boolean;
}

export function StatusBadge({ status, className, showDot = true }: StatusBadgeProps) {
  if (!status) {
    return (
      <div className={cn("inline-flex items-center gap-2", className)}>
        {showDot && <div className="h-1.5 w-1.5 rounded-full bg-muted-foreground/20" />}
        <span className="text-[11px] font-black text-muted-foreground/40 uppercase tracking-tighter">
          No Status
        </span>
      </div>
    );
  }

  return (
    <div className={cn("inline-flex items-center gap-2", className)}>
      {showDot && (
        <div 
          className="h-1.5 w-1.5 rounded-full shadow-[0_0_8px_rgba(0,0,0,0.1)]" 
          style={{ backgroundColor: status.color || '#888' }} 
        />
      )}
      <span 
        className="text-[11px] font-black tracking-tight transition-colors"
        style={{ color: status.color || 'inherit' }}
      >
        {status.name}
      </span>
    </div>
  );
}
