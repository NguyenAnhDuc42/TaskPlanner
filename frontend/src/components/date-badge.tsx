import { cn } from "@/lib/utils";
import { Calendar } from "lucide-react";
import { isBefore, startOfDay } from "date-fns";

interface DateBadgeProps {
  readonly startDate?: string | null;
  readonly dueDate?: string | null;
  readonly className?: string;
  readonly onClick?: () => void;
}

const formatShort = (dateStr: string) => {
  const d = new Date(dateStr);
  return d.toLocaleDateString("en-US", { month: "short", day: "numeric" });
};

export function DateBadge({ startDate, dueDate, className, onClick }: Readonly<DateBadgeProps>) {
  if (!startDate && !dueDate) return null;

  const isOverdue = dueDate ? isBefore(startOfDay(new Date(dueDate)), startOfDay(new Date())) : false;

  let text = "";
  if (startDate && dueDate) {
    text = `${formatShort(startDate)} - ${formatShort(dueDate)}`;
  } else if (startDate) {
    text = `${formatShort(startDate)} →`;
  } else if (dueDate) {
    text = `→ ${formatShort(dueDate)}`;
  }

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
        "flex items-center h-5 gap-1.5 px-2 rounded-sm text-[10px] font-semibold transition-colors cursor-default outline-none focus-visible:ring-1 focus-visible:ring-ring",
        isOverdue
          ? "bg-destructive/10 text-destructive border border-destructive/20 hover:bg-destructive/20"
          : "bg-muted/50 text-muted-foreground border border-border/50 hover:bg-muted/80",
        onClick && "cursor-pointer",
        className
      )}
    >
      <Calendar className="h-3 w-3 opacity-70" />
      <span>{text}</span>
    </div>
  );
}
