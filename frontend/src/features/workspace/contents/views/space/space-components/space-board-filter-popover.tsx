import { useState } from "react";
import { ListFilter } from "lucide-react";
import { Priority } from "@/types/priority";
import { PriorityBadge } from "@/components/priority-badge";
import { DateFilterField } from "@/components/date-filter-field";
import type { SpaceBoardFilter } from "../space-board-types";
import type { Status } from "@/types/status";
import type { MemberRecord } from "@/types/workspace/member-record";
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
  members: MemberRecord[];
}

export function SpaceBoardFilterPopover({ filter, onChange, statuses, members }: SpaceBoardFilterPopoverProps) {
  const [openDateField, setOpenDateField] = useState<"start" | "due" | null>(null);
  const activeCount =
    (filter.priorities?.length ?? 0) +
    (filter.statusIds?.length ?? 0) +
    (filter.assigneeIds?.length ?? 0) +
    (filter.startDate ? 1 : 0) +
    (filter.dueDate ? 1 : 0) +
    (filter.hideArchived ? 1 : 0);

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

  const toggleAssignee = (value: string) => {
    const current = filter.assigneeIds ?? [];
    const next = current.includes(value) ? current.filter(x => x !== value) : [...current, value];
    onChange({ ...filter, assigneeIds: next.length > 0 ? next : undefined });
  };

  return (
    <DropdownMenu onOpenChange={(o) => { if (!o) setOpenDateField(null); }}>
      <DropdownMenuTrigger asChild>
        <button
          title="Filter"
          className={`relative h-7 w-7 flex items-center justify-center rounded-md transition-colors shrink-0 ${
            activeCount > 0
              ? "bg-primary/10 text-primary hover:bg-primary/20"
              : "hover:bg-muted/50 text-muted-foreground"
          }`}
        >
          <ListFilter className="h-3.5 w-3.5" />
          {activeCount > 0 && (
            <span className="absolute -top-0.5 -right-0.5 h-3.5 min-w-3.5 px-0.5 rounded-full bg-primary text-primary-foreground text-[8px] font-bold flex items-center justify-center">
              {activeCount}
            </span>
          )}
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

        {/* Assignee */}
        {members.length > 0 && (
          <DropdownMenuSub>
            <DropdownMenuSubTrigger>
              Assignee
              {filter.assigneeIds?.length ? (
                <span className="ml-1 text-[10px] text-muted-foreground bg-muted px-1.5 rounded-full">{filter.assigneeIds.length}</span>
              ) : null}
            </DropdownMenuSubTrigger>
            <DropdownMenuSubContent className="w-48 max-h-72 overflow-y-auto">
              {members.map(m => (
                <DropdownMenuCheckboxItem
                  key={m.id}
                  checked={filter.assigneeIds?.includes(m.id) ?? false}
                  onCheckedChange={() => toggleAssignee(m.id)}
                  onSelect={(e) => e.preventDefault()}
                >
                  <span className="truncate">{m.name}</span>
                </DropdownMenuCheckboxItem>
              ))}
            </DropdownMenuSubContent>
          </DropdownMenuSub>
        )}

        <DropdownMenuSeparator />
        <DropdownMenuCheckboxItem
          checked={filter.hideArchived ?? false}
          onCheckedChange={(checked) => onChange({ ...filter, hideArchived: checked ? true : undefined })}
          onSelect={(e) => e.preventDefault()}
        >
          Hide archived
        </DropdownMenuCheckboxItem>

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
