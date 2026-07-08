import * as React from "react";
import { Priority } from "@/types/priority";
import { Flag } from "lucide-react";
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

const getPriorityColorClasses = (p: Priority) => {
  switch (p) {
    case Priority.Urgent:
      return "text-red-600 dark:text-red-400 focus:bg-red-500/10 dark:focus:bg-red-500/20 focus:text-red-600 dark:focus:text-red-400";
    case Priority.High:
      return "text-orange-600 dark:text-orange-400 focus:bg-orange-500/10 dark:focus:bg-orange-500/20 focus:text-orange-600 dark:focus:text-orange-400";
    case Priority.Normal:
      return "text-blue-600 dark:text-blue-400 focus:bg-blue-500/10 dark:focus:bg-blue-500/20 focus:text-blue-600 dark:focus:text-blue-400";
    case Priority.Low:
      return "text-muted-foreground focus:bg-muted focus:text-muted-foreground dark:focus:text-muted-foreground";
    case Priority.None:
    default:
      return "text-muted-foreground/50 focus:bg-muted focus:text-muted-foreground/70 dark:focus:text-muted-foreground/70";
  }
};

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
        <DropdownMenuLabel className="text-[10px] uppercase tracking-wider text-muted-foreground font-bold">Priority</DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuRadioGroup value={value} onValueChange={(val) => onChange(val as Priority)}>
          {[
            Priority.None,
            Priority.Low,
            Priority.Normal,
            Priority.High,
            Priority.Urgent,
          ].map((p) => (
            <DropdownMenuRadioItem
              key={p}
              value={p}
              className={cn(
                "flex items-center gap-2 cursor-pointer transition-colors font-medium",
                getPriorityColorClasses(p)
              )}
            >
              <Flag className={cn("h-3.5 w-3.5", p === Priority.None && "opacity-40")} />
              <span>{p === Priority.None ? "No priority" : p}</span>
            </DropdownMenuRadioItem>
          ))}
        </DropdownMenuRadioGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
