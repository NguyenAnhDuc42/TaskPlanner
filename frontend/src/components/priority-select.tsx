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
                className="w-full justify-start border-none bg-transparent hover:bg-transparent p-0 h-auto"
              />
            </DropdownMenuRadioItem>
          ))}
        </DropdownMenuRadioGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
