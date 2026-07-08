import { cn } from "@/lib/utils";
import { Priority } from "@/types/priority";
import { Flag } from "lucide-react";

interface PriorityBadgeProps {
  readonly priority?: Priority;
  readonly className?: string;
  readonly onClick?: () => void;
  readonly showText?: boolean;
  readonly showIcon?: boolean;
}

const getStyle = (p?: Priority) => {
  switch (p) {
    case Priority.Urgent:
      return {
        bg: "bg-red-500 hover:bg-red-600 border-2 border-red-500 dark:bg-red-500/10 dark:hover:bg-red-500/20 dark:border dark:border-transparent",
        text: "text-white font-bold dark:text-red-400 dark:font-semibold",
        icon: "fill-white/90 dark:fill-red-500/50"
      };
    case Priority.High:
      return {
        bg: "bg-orange-500 hover:bg-orange-600 border-2 border-orange-500 dark:bg-orange-500/10 dark:hover:bg-orange-500/20 dark:border dark:border-transparent",
        text: "text-white font-bold dark:text-orange-400 dark:font-semibold",
        icon: "fill-white/90 dark:fill-orange-500/50"
      };
    case Priority.Normal:
      return {
        bg: "bg-blue-500 hover:bg-blue-600 border-2 border-blue-500 dark:bg-blue-500/10 dark:hover:bg-blue-500/20 dark:border dark:border-transparent",
        text: "text-white font-bold dark:text-blue-400 dark:font-semibold",
        icon: "fill-white/90 dark:fill-blue-500/50"
      };
    case Priority.Low:
      return {
        bg: "bg-muted-foreground hover:bg-muted-foreground/90 border-2 border-muted-foreground dark:bg-muted/50 dark:hover:bg-muted/80 dark:border dark:border-transparent",
        text: "text-white font-bold dark:text-muted-foreground dark:font-semibold",
        icon: "opacity-90 dark:opacity-70"
      };
    case Priority.None:
    default:
      return {
        bg: "bg-transparent hover:bg-muted/50 border border-dashed border-muted-foreground/25",
        text: "text-muted-foreground/50 font-semibold",
        icon: "opacity-40"
      };
  }
};

export function PriorityBadge({ priority, className, onClick, showText = true, showIcon = true }: Readonly<PriorityBadgeProps>) {
  const style = getStyle(priority);

  return (
    <div
      onClick={(e) => {
        if (onClick) {
          e.stopPropagation();
          onClick();
        }
      }}
      onKeyDown={(e) => {
        if (onClick && (e.key === "Enter" || e.key === " ")) {
          e.stopPropagation();
          onClick();
        }
      }}
      tabIndex={onClick ? 0 : undefined}
      role={onClick ? "button" : undefined}
      className={cn(
        "flex items-center h-5 gap-1.5 px-2 rounded-sm text-[10px] font-semibold transition-colors duration-300 cursor-default outline-none focus-visible:ring-1 focus-visible:ring-ring",
        style.bg,
        style.text,
        onClick && "cursor-pointer",
        className
      )}
    >
      {showIcon && <Flag className={cn("h-3 w-3", style.icon)} />}
      {showText && <span>{priority && priority !== Priority.None ? priority : "No priority"}</span>}
    </div>
  );
}
