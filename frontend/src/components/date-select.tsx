import * as React from "react";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Calendar as ShadcnCalendar } from "@/components/ui/calendar";
import { DateBadge } from "@/components/date-badge";
import { Calendar } from "lucide-react";
import { cn } from "@/lib/utils";

interface DateSelectProps {
  startDate?: string | null;
  dueDate?: string | null;
  onStartDateChange: (date: Date | undefined) => void;
  onDueDateChange: (date: Date | undefined) => void;
  align?: "start" | "center" | "end";
  triggerClassName?: string;
  size?: "sm" | "md";
}

export function DateSelect({
  startDate,
  dueDate,
  onStartDateChange,
  onDueDateChange,
  align = "start",
  triggerClassName,
  size = "md",
}: Readonly<DateSelectProps>) {
  const isSm = size === "sm";

  return (
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
              "flex items-center gap-1.5 px-2 rounded-sm bg-muted/30 text-muted-foreground/50 transition-colors cursor-pointer select-none border border-border/5",
              isSm 
                ? "h-5 text-[9px] font-bold hover:bg-muted/50" 
                : "h-7 px-2.5 rounded-md border-border/50 bg-muted/10 hover:bg-muted/20 text-xs"
            )}>
              <Calendar className={isSm ? "h-2.5 w-2.5 opacity-50" : "h-3.5 w-3.5 opacity-60"} />
              <span>No Date</span>
            </div>
          )}
        </button>
      </PopoverTrigger>
      <PopoverContent
        className="w-auto p-3 border border-border shadow-xl rounded-xl bg-popover flex flex-col gap-3"
        align={align}
        onClick={(e) => e.stopPropagation()}
        onPointerDown={(e) => e.stopPropagation()}
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
  );
}
