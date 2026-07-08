import { ListFilter, Folder } from "lucide-react";
import { Priority } from "@/types/priority";
import { PriorityBadge } from "@/components/priority-badge";
import { DynamicIcon } from "@/components/dynamic-icon";
import { DateFilterField } from "@/components/date-filter-field";
import type { SpaceBoardFilter } from "../space-board-types";
import type { FolderRecord } from "@/types/projects";
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

const NO_FOLDER_ID = "__none__";

interface SpaceBoardFilterPopoverProps {
  filter: SpaceBoardFilter;
  onChange: (filter: SpaceBoardFilter) => void;
  folders: FolderRecord[];
}

export function SpaceBoardFilterPopover({ filter, onChange, folders }: SpaceBoardFilterPopoverProps) {
  const activeCount =
    (filter.priorities?.length ?? 0) +
    (filter.folderIds?.length ?? 0) +
    (filter.startDate ? 1 : 0) +
    (filter.dueDate ? 1 : 0);

  const toggle = <K extends "priorities" | "folderIds">(key: K, value: string) => {
    const current = (filter[key] ?? []) as string[];
    const next = current.includes(value) ? current.filter(x => x !== value) : [...current, value];
    onChange({ ...filter, [key]: next.length > 0 ? next : undefined });
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          className={`h-7 px-2 flex items-center justify-center gap-1.5 rounded-md transition-colors shrink-0 ${
            activeCount > 0
              ? "bg-primary/10 text-primary hover:bg-primary/20"
              : "hover:bg-muted/50 text-muted-foreground"
          }`}
        >
          <ListFilter className="h-3.5 w-3.5" />
          {activeCount > 0 && <span className="text-[10px] font-bold">{activeCount}</span>}
        </button>
      </DropdownMenuTrigger>

      <DropdownMenuContent align="end" className="w-48">
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
                onCheckedChange={() => toggle("priorities", p)}
                onSelect={(e) => e.preventDefault()}
              >
                <PriorityBadge priority={p} />
              </DropdownMenuCheckboxItem>
            ))}
          </DropdownMenuSubContent>
        </DropdownMenuSub>

        {/* Folder */}
        <DropdownMenuSub>
          <DropdownMenuSubTrigger>
            Folder
            {filter.folderIds?.length ? (
              <span className="ml-1 text-[10px] text-muted-foreground bg-muted px-1.5 rounded-full">{filter.folderIds.length}</span>
            ) : null}
          </DropdownMenuSubTrigger>
          <DropdownMenuSubContent className="w-52">
            <DropdownMenuCheckboxItem
              checked={filter.folderIds?.includes(NO_FOLDER_ID) ?? false}
              onCheckedChange={() => toggle("folderIds", NO_FOLDER_ID)}
              onSelect={(e) => e.preventDefault()}
            >
              <div className="flex items-center gap-2">
                <Folder className="h-3.5 w-3.5 text-muted-foreground" />
                <span className="text-xs italic text-muted-foreground">No folder</span>
              </div>
            </DropdownMenuCheckboxItem>
            {folders.length > 0 && <DropdownMenuSeparator />}
            {folders.map(f => (
              <DropdownMenuCheckboxItem
                key={f.id}
                checked={filter.folderIds?.includes(f.id) ?? false}
                onCheckedChange={() => toggle("folderIds", f.id)}
                onSelect={(e) => e.preventDefault()}
              >
                <div className="flex items-center gap-2 overflow-hidden">
                  <DynamicIcon name={f.icon || "Folder"} size={12} color={f.color || undefined} />
                  <span className="text-xs truncate">{f.name}</span>
                </div>
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
