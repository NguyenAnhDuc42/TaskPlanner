import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Calendar as ShadcnCalendar } from "@/components/ui/calendar";
import { DateBadge } from "@/components/date-badge";
import { Calendar, X } from "lucide-react";
import { cn } from "@/lib/utils";

interface DateSelectProps {
  startDate?: string | null;
  dueDate?: string | null;
  onStartDateChange: (date: Date | undefined) => void;
  onDueDateChange: (date: Date | undefined) => void;
  onClearDates?: () => void;
  align?: "start" | "center" | "end";
  triggerClassName?: string;
  size?: "sm" | "md";
}

export function DateSelect({
  startDate,
  dueDate,
  onStartDateChange,
  onDueDateChange,
  onClearDates,
  align = "start",
  triggerClassName,
  size = "md",
}: Readonly<DateSelectProps>) {
  const isSm = size === "sm";

  return (
    <div className="inline-flex items-center group">
      <Popover>
        <PopoverTrigger asChild>
          <button 
            type="button" 
            className="outline-none focus:outline-none shrink-0"
            onClick={(e) => e.stopPropagation()}
            onPointerDown={(e) => e.stopPropagation()}
          >
            {startDate || dueDate ? (
              <DateBadge
                startDate={startDate}
                dueDate={dueDate}
                className={triggerClassName}
              />
            ) : (
              <div className={cn(
                "flex items-center gap-1.5 px-2 rounded-sm text-muted-foreground/50 transition-colors cursor-pointer select-none border border-dashed border-muted-foreground/25",
                isSm
                  ? "h-5 text-[9px] font-bold hover:bg-muted/30"
                  : "h-6 text-[10px] font-semibold hover:bg-muted/40"
              )}>
                <Calendar className={isSm ? "h-2.5 w-2.5 opacity-70" : "h-3 w-3 opacity-60"} />
                <span>No Date</span>
              </div>
            )}
          </button>
        </PopoverTrigger>
        <PopoverContent
          className="w-auto p-3 border border-border shadow-xl rounded-md bg-popover flex flex-col gap-3"
          align={align}
        >
          <div className="flex gap-4">
            <div className="flex flex-col gap-1.5">
              <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider">Start Date</span>
              <ShadcnCalendar
                mode="single"
                selected={startDate ? new Date(startDate) : undefined}
                onSelect={onStartDateChange}
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider">Due Date</span>
              <ShadcnCalendar
                mode="single"
                selected={dueDate ? new Date(dueDate) : undefined}
                onSelect={onDueDateChange}
              />
            </div>
          </div>
        </PopoverContent>
      </Popover>
      {(startDate || dueDate) && (
        <div
          role="button"
          tabIndex={0}
          className="w-0 group-hover:w-4 ml-0 group-hover:ml-1 opacity-0 group-hover:opacity-100 overflow-hidden flex items-center justify-center p-0.5 rounded-full bg-muted hover:bg-destructive hover:text-destructive-foreground transition-all duration-150 cursor-pointer border border-border/20 shrink-0"
          onClick={(e) => {
            e.stopPropagation();
            e.preventDefault();
            if (onClearDates) {
              onClearDates();
            } else {
              onStartDateChange(undefined);
              onDueDateChange(undefined);
            }
          }}
          onPointerDown={(e) => e.stopPropagation()}
        >
          <X className="h-3 w-3 shrink-0" />
        </div>
      )}
    </div>
  );
}
