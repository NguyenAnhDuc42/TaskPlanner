import { cn } from "@/lib/utils";
import { type Status } from "@/types/status";
import { StatusCategory } from "@/types/status-category";
import { CircleDashed, CheckCircle2, AlertCircle } from "lucide-react";

// Custom icon: a dashed circle with a larger filled dot in the center
function ActiveCircleDot(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      {...props}
    >
      <path d="M10.1 2.18a9.93 9.93 0 0 1 3.8 0" />
      <path d="M17.6 3.81a9.93 9.93 0 0 1 2.59 2.59" />
      <path d="M21.8 10.1a9.93 9.93 0 0 1 0 3.8" />
      <path d="M20.2 17.6a9.93 9.93 0 0 1-2.59 2.59" />
      <path d="M13.9 21.8a9.93 9.93 0 0 1-3.8 0" />
      <path d="M6.39 20.2a9.93 9.93 0 0 1-2.59-2.59" />
      <path d="M2.18 13.9a9.93 9.93 0 0 1 0-3.8" />
      <path d="M3.81 6.39a9.93 9.93 0 0 1 2.59-2.59" />
      <circle cx="12" cy="12" r="5" fill="currentColor" stroke="none" />
    </svg>
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
      <div className={cn("flex items-center gap-1 text-[10px] text-muted-foreground font-medium", className)}>
        {showIcon && <CircleDashed className="h-3 w-3 opacity-50" />}
        <span>No Status</span>
      </div>
    );
  }

  const statusColor = status.color || "currentColor";
  
  const Icon = status.category === StatusCategory.NotStarted ? CircleDashed
    : status.category === StatusCategory.Active ? ActiveCircleDot
    : status.category === StatusCategory.Done ? CheckCircle2
    : status.category === StatusCategory.Closed ? AlertCircle
    : CircleDashed;

  if (variant === "outline") {
    return (
      <div 
        className={cn("flex items-center gap-1.5 px-2 py-0.5 rounded-md border text-[10px] font-semibold transition-all duration-300", className)}
        style={{ 
          color: statusColor,
          borderColor: `${statusColor}33`,
          backgroundColor: `${statusColor}0a`
        }}
      >
        {showIcon && (
          <Icon className="h-3 w-3" />
        )}
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
        {showIcon && (
          <Icon className="h-3 w-3" />
        )}
        <span>{status.name}</span>
      </div>
    );
  }

  return (
    <div 
      className={cn("flex items-center gap-1 text-[10px] font-medium transition-colors duration-300", className)}
      style={{ color: statusColor }}
    >
      {showIcon && (
        <Icon className="h-3 w-3" />
      )}
      <span>{status.name}</span>
    </div>
  );
}
