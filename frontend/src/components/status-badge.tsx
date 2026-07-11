import { cn } from "@/lib/utils";
import { type Status } from "@/types/status";
import { CircleDashed } from "lucide-react";

function StatusDot({ color, className }: { color: string; className?: string }) {
  return (
    <span
      className={cn("inline-block h-2 w-2 rounded-full shrink-0", className)}
      style={{ backgroundColor: color }}
    />
  );
}

interface StatusBadgeProps {
  status?: Status | null;
  className?: string;
  showIcon?: boolean;
  variant?: "text" | "outline" | "pill";
}

export function StatusBadge({ status, className, showIcon = true, variant = "text" }: StatusBadgeProps) {
  if (!status) {
    return (
      <div className={cn(
        "flex items-center h-5 gap-1.5 px-2 rounded-sm text-[10px] font-medium text-muted-foreground/50 border border-dashed border-muted-foreground/25",
        className
      )}>
        {showIcon && <CircleDashed className="h-3 w-3 opacity-40" />}
        <span>No Status</span>
      </div>
    );
  }

  const statusColor = status.color || "currentColor";

  if (variant === "outline") {
    return (
      <div
        className={cn("theme-adaptive-badge flex items-center h-5 gap-1.5 px-2 rounded-sm text-[10px] transition-all duration-300", className)}
        style={{
          "--status-color": statusColor,
        } as React.CSSProperties}
      >
        {showIcon && <StatusDot color={statusColor} />}
        <span>{status.name}</span>
      </div>
    );
  }

  if (variant === "pill") {
    return (
      <div
        className={cn("flex items-center gap-1.5 h-5 px-2 rounded-sm text-[10px] font-semibold transition-all duration-300", className)}
        style={{
          color: statusColor,
          backgroundColor: `${statusColor}1a`
        }}
      >
        {showIcon && <StatusDot color={statusColor} />}
        <span>{status.name}</span>
      </div>
    );
  }

  return (
    <div
      className={cn("flex items-center gap-1 text-[10px] font-medium transition-colors duration-300", className)}
      style={{ color: statusColor }}
    >
      {showIcon && <StatusDot color={statusColor} />}
      <span>{status.name}</span>
    </div>
  );
}
