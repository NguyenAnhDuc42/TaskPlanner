import { Search, X, SlidersHorizontal, Workflow, ArrowUpDown } from "lucide-react";
import { useCallback } from "react";
import { cn } from "@/lib/utils";
import type { Status } from "@/types/status";
import type { SpaceBoardFilter, SpaceBoardSortBy } from "../space-board-types";
import { SORT_OPTIONS } from "../space-board-types";
import { SpaceBoardFilterPopover } from "./space-board-filter-popover";
import {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
} from "@/components/ui/dropdown-menu";

interface SpaceFilterBarProps {
  statuses: Status[];
  hiddenStatusIds: string[];
  setHiddenStatusIds: React.Dispatch<React.SetStateAction<string[]>>;
  filter: SpaceBoardFilter;
  onFilterChange: (f: SpaceBoardFilter) => void;
  searchInput: string;
  onSearchChange: (v: string) => void;
  isFullyLoaded: boolean;
  hideUnclassified: boolean;
  onToggleUnclassified: () => void;
  onOpenWorkflow?: () => void;
  sortBy: SpaceBoardSortBy;
  onSortByChange: (sortBy: SpaceBoardSortBy) => void;
}

export function SpaceFilterBar({
  statuses,
  hiddenStatusIds,
  setHiddenStatusIds,
  filter,
  onFilterChange,
  searchInput,
  onSearchChange,
  isFullyLoaded,
  hideUnclassified,
  onToggleUnclassified,
  onOpenWorkflow,
  sortBy,
  onSortByChange,
}: Readonly<SpaceFilterBarProps>) {
  const toggleStatusVisibility = useCallback(
    (statusId: string) => {
      setHiddenStatusIds((prev) =>
        prev.includes(statusId) ? prev.filter((id) => id !== statusId) : [...prev, statusId],
      );
    },
    [setHiddenStatusIds],
  );

  const hiddenCount = hiddenStatusIds.length + (hideUnclassified ? 1 : 0);

  return (
    <>
      {/* Columns — a plain list of statuses, click a row to show/hide it as a column. No
          checkboxes, no per-row extras — just the list, plus Manage Statuses at the bottom to
          open the Workflow Manager form. */}
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
          {statuses.map((status) => {
            const isHidden = hiddenStatusIds.includes(status.id!);
            return (
              <DropdownMenuItem
                key={status.id}
                onSelect={(e) => { e.preventDefault(); toggleStatusVisibility(status.id!); }}
                className={cn("gap-2", isHidden && "opacity-40")}
              >
                <span className="h-2 w-2 rounded-full shrink-0" style={{ backgroundColor: status.color }} />
                <span className="truncate">{status.name}</span>
              </DropdownMenuItem>
            );
          })}

          <DropdownMenuItem
            onSelect={(e) => { e.preventDefault(); onToggleUnclassified(); }}
            className={cn("gap-2", hideUnclassified && "opacity-40")}
          >
            <span className="h-2 w-2 rounded-full shrink-0 bg-muted-foreground/40" />
            Unclassified
          </DropdownMenuItem>

          {onOpenWorkflow && (
            <>
              <DropdownMenuSeparator />
              <DropdownMenuItem onSelect={onOpenWorkflow} className="gap-2">
                <Workflow className="h-3.5 w-3.5 text-muted-foreground/70" />
                Manage Statuses
              </DropdownMenuItem>
            </>
          )}
        </DropdownMenuContent>
      </DropdownMenu>

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

      {/* Sort + Filter — icon-only, grouped together as the two "how tasks are arranged" controls. */}
      <div className="flex items-center gap-0.5">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button
              title="Sort"
              className={cn(
                "h-7 w-7 flex items-center justify-center rounded-md transition-colors shrink-0",
                sortBy !== "priority"
                  ? "bg-primary/10 text-primary hover:bg-primary/20"
                  : "hover:bg-muted/50 text-muted-foreground"
              )}
            >
              <ArrowUpDown className="h-3.5 w-3.5" />
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-48">
            <DropdownMenuLabel>Sort by</DropdownMenuLabel>
            <DropdownMenuRadioGroup value={sortBy} onValueChange={(v) => onSortByChange(v as SpaceBoardSortBy)}>
              {SORT_OPTIONS.map((opt) => (
                <DropdownMenuRadioItem key={opt.value} value={opt.value}>
                  {opt.label}
                </DropdownMenuRadioItem>
              ))}
            </DropdownMenuRadioGroup>
          </DropdownMenuContent>
        </DropdownMenu>

        <SpaceBoardFilterPopover filter={filter} onChange={onFilterChange} statuses={statuses} />
      </div>
    </>
  );
}
