import * as React from "react";
import { Priority } from "@/types/priority";
import { PriorityBadge } from "@/components/priority-badge";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/utils";

interface PrioritySelectProps {
  readonly value?: Priority;
  readonly onChange: (priority: Priority) => void;
  readonly align?: "start" | "end" | "center";
  readonly trigger: React.ReactNode;
}

export function PrioritySelect({
  value,
  onChange,
  align = "start",
  trigger,
}: PrioritySelectProps) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        {trigger}
      </DropdownMenuTrigger>
      <DropdownMenuContent
        align={align}
        className="w-40"
        onClick={(e) => e.stopPropagation()}
        onPointerDown={(e) => e.stopPropagation()}
      >
        <DropdownMenuLabel>Priority</DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuRadioGroup value={value} onValueChange={(val) => onChange(val as Priority)}>
          {[
            Priority.Low,
            Priority.Normal,
            Priority.High,
            Priority.Urgent,
          ].map((p) => (
            <DropdownMenuRadioItem
              key={p}
              value={p}
              className={cn(value === p && "bg-muted shadow-sm")}
            >
              <PriorityBadge
                priority={p}
                className="w-full justify-start pointer-events-none"
              />
            </DropdownMenuRadioItem>
          ))}
        </DropdownMenuRadioGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
