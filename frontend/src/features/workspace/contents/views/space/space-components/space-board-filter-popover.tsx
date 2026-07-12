import { useState } from "react";
import { ListFilter } from "lucide-react";
import { Priority } from "@/types/priority";
import { PriorityBadge } from "@/components/priority-badge";
import { DateFilterField } from "@/components/date-filter-field";
import type { SpaceBoardFilter } from "../space-board-types";
import type { Status } from "@/types/status";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
  DropdownMenuCheckboxItem,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubTrigger,
  DropdownMenuSubContent,
  DropdownMenuLabel,
} from "@/components/ui/dropdown-menu";

interface SpaceBoardFilterPopoverProps {
  filter: SpaceBoardFilter;
  onChange: (filter: SpaceBoardFilter) => void;
  statuses: Status[];
}

export function SpaceBoardFilterPopover({ filter, onChange, statuses }: SpaceBoardFilterPopoverProps) {
  const [openDateField, setOpenDateField] = useState<"start" | "due" | null>(null);
  const activeCount =
    (filter.priorities?.length ?? 0) +
    (filter.statusIds?.length ?? 0) +
    (filter.startDate ? 1 : 0) +
    (filter.dueDate ? 1 : 0);

  const togglePriority = (value: Priority) => {
    const current = filter.priorities ?? [];
    const next = current.includes(value) ? current.filter(x => x !== value) : [...current, value];
    onChange({ ...filter, priorities: next.length > 0 ? next : undefined });
  };

  const toggleStatus = (value: string) => {
    const current = filter.statusIds ?? [];
    const next = current.includes(value) ? current.filter(x => x !== value) : [...current, value];
    onChange({ ...filter, statusIds: next.length > 0 ? next : undefined });
  };

  return (
    <DropdownMenu onOpenChange={(o) => { if (!o) setOpenDateField(null); }}>
      <DropdownMenuTrigger asChild>
        <button
          className={`h-7 px-2 flex items-center justify-center gap-1.5 rounded-md transition-colors shrink-0 ${
            activeCount > 0
              ? "bg-primary/10 text-primary hover:bg-primary/20"
              : "hover:bg-muted/50 text-muted-foreground"
          }`}
        >
          <ListFilter className="h-3.5 w-3.5" />
          <span className="text-[10px] font-semibold">Filter</span>
          {activeCount > 0 && <span className="text-[10px] font-bold">{activeCount}</span>}
        </button>
      </DropdownMenuTrigger>

      <DropdownMenuContent align="end" className="w-60">
        <DropdownMenuLabel className="flex items-center justify-between">
          <span>Filters</span>
          {activeCount > 0 && (
            <button
              onClick={(e) => { e.preventDefault(); onChange({ search: filter.search }); }}
              className="text-[9px] text-muted-foreground hover:text-foreground normal-case font-medium transition-colors cursor-pointer"
            >
              Clear All
            </button>
          )}
        </DropdownMenuLabel>

        <DropdownMenuSeparator />

        {/* Priority */}
        <DropdownMenuSub>
          <DropdownMenuSubTrigger>
            Priority
            {filter.priorities?.length ? (
              <span className="ml-1 text-[10px] text-muted-foreground bg-muted px-1.5 rounded-full">{filter.priorities.length}</span>
            ) : null}
          </DropdownMenuSubTrigger>
          <DropdownMenuSubContent className="w-40">
            {Object.values(Priority).map(p => (
              <DropdownMenuCheckboxItem
                key={p}
                checked={filter.priorities?.includes(p) ?? false}
                onCheckedChange={() => togglePriority(p)}
                onSelect={(e) => e.preventDefault()}
              >
                <PriorityBadge priority={p} />
              </DropdownMenuCheckboxItem>
            ))}
          </DropdownMenuSubContent>
        </DropdownMenuSub>

        {/* Status — filters which tasks show, independent of which columns are rendered
            (that's the Columns dropdown's job, not this one). */}
        <DropdownMenuSub>
          <DropdownMenuSubTrigger>
            Status
            {filter.statusIds?.length ? (
              <span className="ml-1 text-[10px] text-muted-foreground bg-muted px-1.5 rounded-full">{filter.statusIds.length}</span>
            ) : null}
          </DropdownMenuSubTrigger>
          <DropdownMenuSubContent className="w-48">
            {statuses.map(s => (
              <DropdownMenuCheckboxItem
                key={s.id}
                checked={filter.statusIds?.includes(s.id!) ?? false}
                onCheckedChange={() => toggleStatus(s.id!)}
                onSelect={(e) => e.preventDefault()}
              >
                <span className="flex items-center gap-2 overflow-hidden">
                  <span className="h-2 w-2 rounded-full shrink-0" style={{ backgroundColor: s.color }} />
                  <span className="truncate">{s.name}</span>
                </span>
              </DropdownMenuCheckboxItem>
            ))}
          </DropdownMenuSubContent>
        </DropdownMenuSub>

        {/* Dates */}
        <DropdownMenuSeparator />
        <DropdownMenuLabel>Dates</DropdownMenuLabel>
        <div className="px-2 py-1.5 space-y-2">
          <DateFilterField
            label="Start Date (From)"
            value={filter.startDate}
            onChange={(startDate) => onChange({ ...filter, startDate })}
            open={openDateField === "start"}
            onOpenChange={(o) => setOpenDateField(o ? "start" : null)}
          />
          <DateFilterField
            label="Due Date (Until)"
            value={filter.dueDate}
            onChange={(dueDate) => onChange({ ...filter, dueDate })}
            open={openDateField === "due"}
            onOpenChange={(o) => setOpenDateField(o ? "due" : null)}
          />
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
