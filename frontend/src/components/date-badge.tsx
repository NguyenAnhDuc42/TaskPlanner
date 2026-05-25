import { cn } from "@/lib/utils";
import { Calendar } from "lucide-react";

interface DateBadgeProps {
  startDate?: string | null;
  dueDate?: string | null;
  className?: string;
  onClick?: () => void;
}

export function DateBadge({ startDate, dueDate, className, onClick }: DateBadgeProps) {
  if (!startDate && !dueDate) return null;

  const formatShort = (dateStr: string) => {
    const d = new Date(dateStr);
    return d.toLocaleDateString("en-US", { month: "short", day: "numeric" });
  };

  let text = "";
  if (startDate && dueDate) {
    text = `${formatShort(startDate)} - ${formatShort(dueDate)}`;
  } else if (startDate) {
    text = formatShort(startDate);
  } else if (dueDate) {
    text = formatShort(dueDate);
  }

  return (
    <div 
      onClick={(e) => {
        if (onClick) {
          e.stopPropagation();
          onClick();
        }
      }}
      className={cn(
        "flex items-center h-5 gap-1.5 px-2 rounded-sm bg-muted/50 text-[10px] text-muted-foreground font-semibold hover:bg-muted/80 transition-colors cursor-default", 
        onClick && "cursor-pointer",
        className
      )}
    >
      <Calendar className="h-3 w-3 opacity-70" />
      <span>{text}</span>
    </div>
  );
}
