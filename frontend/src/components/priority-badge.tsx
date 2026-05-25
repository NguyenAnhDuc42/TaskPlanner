import { cn } from "@/lib/utils";
import { Priority } from "@/types/priority";
import { Flag } from "lucide-react";

interface PriorityBadgeProps {
  priority?: Priority;
  className?: string;
  onClick?: () => void;
  showText?: boolean;
  showIcon?: boolean;
}

export function PriorityBadge({ priority, className, onClick, showText = true, showIcon = true }: PriorityBadgeProps) {
  const getStyle = (p?: Priority) => {
    switch (p) {
      case Priority.Urgent:
        return {
          bg: "bg-red-500/10 hover:bg-red-500/20",
          text: "text-red-500",
          icon: "fill-red-500/50"
        };
      case Priority.High:
        return {
          bg: "bg-orange-500/10 hover:bg-orange-500/20",
          text: "text-orange-500",
          icon: "fill-orange-500/50"
        };
      case Priority.Normal:
        return {
          bg: "bg-blue-500/10 hover:bg-blue-500/20",
          text: "text-blue-500",
          icon: "fill-blue-500/50"
        };
      case Priority.Low:
      default:
        return {
          bg: "bg-muted/50 hover:bg-muted/80",
          text: "text-muted-foreground",
          icon: "opacity-70"
        };
    }
  };

  const style = getStyle(priority);
  
  return (
    <div 
      onClick={(e) => {
        if (onClick) {
          e.stopPropagation();
          onClick();
        }
      }}
      className={cn(
        "flex items-center h-5 gap-1.5 px-2 rounded-sm text-[10px] font-semibold transition-colors duration-300 cursor-default",
        style.bg,
        style.text,
        onClick && "cursor-pointer",
        className
      )}
    >
      {showIcon && <Flag className={cn("h-3 w-3", style.icon)} />}
      {showText && <span>{priority || "Normal"}</span>}
    </div>
  );
}
