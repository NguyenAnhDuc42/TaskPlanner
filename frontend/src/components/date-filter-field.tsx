import * as React from "react";
import { Calendar as CalendarIcon } from "lucide-react";
import { Calendar as ShadcnCalendar } from "@/components/ui/calendar";
import { cn } from "@/lib/utils";

interface DateFilterFieldProps {
  readonly label: string;
  readonly value?: string;
  readonly onChange: (iso: string | undefined) => void;
}

export function DateFilterField({ label, value, onChange }: DateFilterFieldProps) {
  const [open, setOpen] = React.useState(false);
  const selected = value ? new Date(value) : undefined;

  return (
    <div className="flex flex-col gap-1">
      <span className="text-[10px] text-muted-foreground">{label}</span>
      <button
        type="button"
        onClick={(e) => { e.stopPropagation(); setOpen((o) => !o); }}
        className="h-6 w-full flex items-center gap-1.5 px-1.5 rounded border border-border/40 bg-muted/30 hover:bg-muted/50 transition-colors text-left"
      >
        <CalendarIcon className="h-3 w-3 opacity-60 shrink-0" />
        <span className={cn("text-[10px]", !selected && "text-muted-foreground/50")}>
          {selected ? selected.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" }) : "Select date"}
        </span>
      </button>
      {open && (
        <div
          className="mt-1 border border-border rounded-md bg-popover shadow-xl"
          onClick={(e) => e.stopPropagation()}
        >
          <ShadcnCalendar
            mode="single"
            selected={selected}
            onSelect={(date) => {
              onChange(date ? date.toISOString() : undefined);
              setOpen(false);
            }}
          />
        </div>
      )}
    </div>
  );
}
