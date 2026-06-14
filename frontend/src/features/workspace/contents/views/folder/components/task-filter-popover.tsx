import * as React from "react";
import { ListFilter } from "lucide-react";
import { Priority } from "@/types/priority";
import { PriorityBadge } from "@/components/priority-badge";
import { StatusBadge } from "@/components/status-badge";
import { Input } from "@/components/ui/input";
import type { TaskFilter } from "../folder-api";
import type { Status } from "@/types/status";
import type { MemberRecord } from "@/types/workspace";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
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
  members: MemberRecord[];
}

export function TaskFilterPopover({ filter, onChange, statuses, members }: TaskFilterPopoverProps) {
  const activeFilterCount =
    (filter.statusIds?.length || 0) +
    (filter.priorities?.length || 0) +
    (filter.assigneeIds?.length || 0) +
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

  const toggleAssignee = (id: string) => {
    const current = filter.assigneeIds || [];
    const newAssignees = current.includes(id) ? current.filter(x => x !== id) : [...current, id];
    onChange({ ...filter, assigneeIds: newAssignees.length > 0 ? newAssignees : undefined });
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

        {/* Assignees */}
        {members.length > 0 && (
          <DropdownMenuSub>
            <DropdownMenuSubTrigger>
              Assignees
              {filter.assigneeIds?.length ? (
                <span className="ml-1 text-[10px] text-muted-foreground bg-muted px-1.5 rounded-full">{filter.assigneeIds.length}</span>
              ) : null}
            </DropdownMenuSubTrigger>
            <DropdownMenuSubContent className="w-56">
              {members.map(m => {
                const assigneeId = m.workspaceMemberId;
                if (!assigneeId) return null;
                
                return (
                <DropdownMenuCheckboxItem
                  key={assigneeId}
                  checked={filter.assigneeIds?.includes(assigneeId) || false}
                  onCheckedChange={() => toggleAssignee(assigneeId)}
                  onSelect={(e) => e.preventDefault()}
                >
                  <div className="flex items-center gap-2 overflow-hidden">
                    <Avatar className="h-4 w-4 border border-border/50 shrink-0">
                      <AvatarImage src={m.avatarUrl || undefined} />
                      <AvatarFallback className="text-[8px] bg-primary/10 text-primary font-medium">
                        {m.name.substring(0, 2).toUpperCase()}
                      </AvatarFallback>
                    </Avatar>
                    <span className="text-xs truncate">{m.name}</span>
                  </div>
                </DropdownMenuCheckboxItem>
                );
              })}
            </DropdownMenuSubContent>
          </DropdownMenuSub>
        )}

        {/* Dates */}
        <DropdownMenuSeparator />
        <DropdownMenuLabel>Dates</DropdownMenuLabel>
        <div className="px-2 py-1.5 space-y-2">
          <div className="flex flex-col gap-1">
            <span className="text-[10px] text-muted-foreground">Start Date (From)</span>
            <Input
              type="date"
              className="h-6 text-[10px] px-1.5"
              value={filter.startDate ? filter.startDate.split("T")[0] : ""}
              onChange={(e) => {
                const date = e.target.value ? new Date(e.target.value).toISOString() : undefined;
                onChange({ ...filter, startDate: date });
              }}
              onClick={(e) => e.stopPropagation()} // prevent closing
            />
          </div>
          <div className="flex flex-col gap-1">
            <span className="text-[10px] text-muted-foreground">Due Date (Until)</span>
            <Input
              type="date"
              className="h-6 text-[10px] px-1.5"
              value={filter.dueDate ? filter.dueDate.split("T")[0] : ""}
              onChange={(e) => {
                const date = e.target.value ? new Date(e.target.value).toISOString() : undefined;
                onChange({ ...filter, dueDate: date });
              }}
              onClick={(e) => e.stopPropagation()}
            />
          </div>
        </div>

      </DropdownMenuContent>
    </DropdownMenu>
  );
}
