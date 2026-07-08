
import { ListFilter } from "lucide-react";
import { Priority } from "@/types/priority";
import { PriorityBadge } from "@/components/priority-badge";
import { StatusBadge } from "@/components/status-badge";
import { DateFilterField } from "@/components/date-filter-field";
import type { TaskFilter } from "./folder-task-list";
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

interface TaskFilterPopoverProps {
  filter: TaskFilter;
  onChange: (filter: TaskFilter) => void;
  statuses: Status[];
}

export function TaskFilterPopover({ filter, onChange, statuses }: TaskFilterPopoverProps) {
  const activeFilterCount =
    (filter.statusIds?.length || 0) +
    (filter.priorities?.length || 0) +
    (filter.startDate ? 1 : 0) +
    (filter.dueDate ? 1 : 0);

  const toggleStatus = (id: string) => {
    const current = filter.statusIds || [];
    const newStatuses = current.includes(id) ? current.filter(x => x !== id) : [...current, id];
    onChange({ ...filter, statusIds: newStatuses.length > 0 ? newStatuses : undefined });
  };

  const togglePriority = (p: Priority) => {
    const current = filter.priorities || [];
    const newPriorities = current.includes(p) ? current.filter(x => x !== p) : [...current, p];
    onChange({ ...filter, priorities: newPriorities.length > 0 ? newPriorities : undefined });
  };



  const handleClearAll = () => {
    onChange({ search: filter.search }); // keep search, clear everything else
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button className={`h-7 px-2 flex items-center justify-center gap-1.5 rounded-md transition-colors shrink-0 relative ${activeFilterCount > 0 ? "bg-primary/10 text-primary hover:bg-primary/20" : "hover:bg-muted/50 text-muted-foreground"}`}>
          <ListFilter className="h-3.5 w-3.5" />
          {activeFilterCount > 0 && (
            <span className="text-[10px] font-bold">{activeFilterCount}</span>
          )}
        </button>
      </DropdownMenuTrigger>
      
      <DropdownMenuContent align="start" className="w-48">
        <DropdownMenuLabel className="flex items-center justify-between">
          <span>Filters</span>
          {activeFilterCount > 0 && (
            <button 
              onClick={(e) => { e.preventDefault(); handleClearAll(); }}
              className="text-[9px] text-muted-foreground hover:text-foreground normal-case font-medium transition-colors cursor-pointer"
            >
              Clear All
            </button>
          )}
        </DropdownMenuLabel>
        
        <DropdownMenuSeparator />

        {/* Status */}
        {statuses.length > 0 && (
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
                  checked={filter.statusIds?.includes(s.id) || false}
                  onCheckedChange={() => toggleStatus(s.id)}
                  onSelect={(e) => e.preventDefault()} // prevent closing on select
                >
                  <StatusBadge status={s} />
                </DropdownMenuCheckboxItem>
              ))}
            </DropdownMenuSubContent>
          </DropdownMenuSub>
        )}

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
                checked={filter.priorities?.includes(p) || false}
                onCheckedChange={() => togglePriority(p)}
                onSelect={(e) => e.preventDefault()}
              >
                <PriorityBadge priority={p} />
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
          />
          <DateFilterField
            label="Due Date (Until)"
            value={filter.dueDate}
            onChange={(dueDate) => onChange({ ...filter, dueDate })}
          />
        </div>

      </DropdownMenuContent>
    </DropdownMenu>
  );
}
