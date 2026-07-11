import { Plus, Search, X, SlidersHorizontal } from "lucide-react";
import { useCallback } from "react";
import { cn } from "@/lib/utils";
import type { Status } from "@/types/status";
import type { FolderRecord } from "@/types/projects";
import type { SpaceBoardFilter } from "../space-board-types";
import { SpaceBoardFilterPopover } from "./space-board-filter-popover";
import {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuCheckboxItem,
  DropdownMenuSeparator,
  DropdownMenuLabel,
} from "@/components/ui/dropdown-menu";

interface SpaceFilterBarProps {
  statuses: Status[];
  hiddenStatusIds: string[];
  setHiddenStatusIds: React.Dispatch<React.SetStateAction<string[]>>;
  folders: FolderRecord[];
  filter: SpaceBoardFilter;
  onFilterChange: (f: SpaceBoardFilter) => void;
  searchInput: string;
  onSearchChange: (v: string) => void;
  onWorkflowOpen?: () => void;
  isFullyLoaded: boolean;
  hideUnclassified: boolean;
  onToggleUnclassified: () => void;
  hasEmptyStatuses: boolean;
  allEmptyHidden: boolean;
  onToggleHideEmpty: () => void;
}

export function SpaceFilterBar({
  statuses,
  hiddenStatusIds,
  setHiddenStatusIds,
  folders,
  filter,
  onFilterChange,
  searchInput,
  onSearchChange,
  onWorkflowOpen,
  isFullyLoaded,
  hideUnclassified,
  onToggleUnclassified,
  hasEmptyStatuses,
  allEmptyHidden,
  onToggleHideEmpty,
}: Readonly<SpaceFilterBarProps>) {
  const toggleStatusVisibility = useCallback(
    (statusId: string) => {
      setHiddenStatusIds((prev) =>
        prev.includes(statusId) ? prev.filter((id) => id !== statusId) : [...prev, statusId],
      );
    },
    [setHiddenStatusIds],
  );

  const isolateStatus = useCallback(
    (statusId: string) => {
      setHiddenStatusIds((prev) => {
        const others = statuses.map((s) => s.id!).filter((id) => id !== statusId);
        const isIsolated = prev.length === others.length && others.every((id) => prev.includes(id));
        return isIsolated ? [] : others;
      });
    },
    [statuses, setHiddenStatusIds],
  );

  const hiddenCount = hiddenStatusIds.length + (hideUnclassified ? 1 : 0) + (allEmptyHidden ? 1 : 0);

  return (
    <div className="px-2 py-1 flex items-center shrink-0 select-none gap-2 bg-card z-10">
      {/* Columns — show/hide/isolate statuses, one dropdown instead of a chip per status */}
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <button
            className={cn(
              "h-7 px-2 flex items-center justify-center gap-1.5 rounded-md transition-colors shrink-0",
              hiddenCount > 0
                ? "bg-primary/10 text-primary hover:bg-primary/20"
                : "hover:bg-muted/50 text-muted-foreground"
            )}
          >
            <SlidersHorizontal className="h-3.5 w-3.5" />
            <span className="text-[10px] font-semibold">Columns</span>
            {hiddenCount > 0 && <span className="text-[10px] font-bold">{hiddenCount}</span>}
          </button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-56">
          <DropdownMenuLabel>Columns</DropdownMenuLabel>
          {statuses.map((status) => (
            <DropdownMenuCheckboxItem
              key={status.id}
              checked={!hiddenStatusIds.includes(status.id!)}
              onCheckedChange={() => toggleStatusVisibility(status.id!)}
              onSelect={(e) => e.preventDefault()}
              className="group justify-between"
            >
              <span className="flex items-center gap-2 overflow-hidden">
                <span className="h-2 w-2 rounded-full shrink-0" style={{ backgroundColor: status.color }} />
                <span className="truncate">{status.name}</span>
              </span>
              <button
                onClick={(e) => { e.stopPropagation(); isolateStatus(status.id!); }}
                className="opacity-0 group-hover:opacity-100 text-[9px] font-semibold text-muted-foreground hover:text-foreground transition-opacity shrink-0"
              >
                Only
              </button>
            </DropdownMenuCheckboxItem>
          ))}

          <DropdownMenuCheckboxItem
            checked={!hideUnclassified}
            onCheckedChange={() => onToggleUnclassified()}
            onSelect={(e) => e.preventDefault()}
          >
            <span className="flex items-center gap-2">
              <span className="h-2 w-2 rounded-full shrink-0 bg-muted-foreground/40" />
              Unclassified
            </span>
          </DropdownMenuCheckboxItem>

          {hasEmptyStatuses && (
            <>
              <DropdownMenuSeparator />
              <DropdownMenuCheckboxItem
                checked={allEmptyHidden}
                onCheckedChange={() => onToggleHideEmpty()}
                onSelect={(e) => e.preventDefault()}
              >
                Hide empty columns
              </DropdownMenuCheckboxItem>
            </>
          )}
        </DropdownMenuContent>
      </DropdownMenu>

      {onWorkflowOpen && (
        <button
          onClick={onWorkflowOpen}
          className="flex items-center justify-center h-6 w-6 rounded-full bg-muted/40 text-muted-foreground hover:bg-muted/80 hover:text-foreground border border-border/30 transition-all cursor-pointer shrink-0"
          title="Manage Workflow"
        >
          <Plus className="h-3 w-3" />
        </button>
      )}

      {/* Search + Filter — right side */}
      <div className="ml-auto flex items-center gap-1.5 shrink-0">
        {!isFullyLoaded && (
          <span className="text-[9px] text-muted-foreground/40 animate-pulse font-medium shrink-0">
            loading…
          </span>
        )}
        <div className="flex items-center gap-2 px-2 h-7 w-36 focus-within:w-48 rounded-md bg-secondary/60 border border-transparent focus-within:border-primary/30 focus-within:bg-secondary transition-all group shadow-inner">
          <Search className="h-3 w-3 text-muted-foreground/40 group-focus-within:text-primary transition-colors shrink-0" />
          <input
            value={searchInput}
            onChange={(e) => onSearchChange(e.target.value)}
            placeholder="Search..."
            className="flex-1 bg-transparent border-none outline-none text-[11px] font-medium text-foreground placeholder:text-muted-foreground/40 transition-all min-w-0"
          />
          {searchInput && (
            <button
              onClick={() => onSearchChange("")}
              className="text-muted-foreground/40 hover:text-foreground transition-colors shrink-0"
            >
              <X className="h-3 w-3" />
            </button>
          )}
        </div>
        <SpaceBoardFilterPopover filter={filter} onChange={onFilterChange} folders={folders} />
      </div>
    </div>
  );
}
