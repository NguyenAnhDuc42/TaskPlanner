import * as React from "react";
import { Priority } from "@/types/priority";
import { PriorityBadge } from "@/components/priority-badge";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";

interface PrioritySelectProps {
  readonly value?: Priority;
  readonly onChange: (priority: Priority) => void;
  readonly align?: "start" | "end" | "center";
  readonly trigger: React.ReactNode;
}

export function PrioritySelect({
  value: _value,
  onChange,
  align = "start",
  trigger,
}: PrioritySelectProps) {
  return (
    <Popover>
      <PopoverTrigger asChild>
        {trigger}
      </PopoverTrigger>
      <PopoverContent
        className="w-32 p-1 bg-popover border border-border shadow-md rounded-md"
        align={align}
        onFocusOutside={(e) => e.preventDefault()}
      >
        <div className="border-b border-border/10 mb-1">
          <span className="text-[8px] font-black uppercase tracking-wider text-muted-foreground/50">Select Priority</span>
        </div>
        <div className="flex flex-col gap-0.5">
          {[
            Priority.Low,
            Priority.Normal,
            Priority.High,
            Priority.Urgent,
          ].map((p) => (
            <button
              key={p}
              type="button"
              className="px-1.5 py-1 text-xs text-left rounded-sm hover:bg-muted transition-colors flex items-center w-full"
              onClick={() => onChange(p)}
            >
              <PriorityBadge
                priority={p}
                className="w-full justify-start border-none bg-transparent hover:bg-transparent"
              />
            </button>
          ))}
        </div>
      </PopoverContent>
    </Popover>
  );
}
