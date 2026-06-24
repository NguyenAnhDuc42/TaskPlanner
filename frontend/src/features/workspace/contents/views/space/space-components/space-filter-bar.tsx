import { Plus, EyeOff, Eye } from "lucide-react";
import { StatusBadge } from "@/components/status-badge";
import { useCallback } from "react";
import { cn } from "@/lib/utils";
import type { Status } from "@/types/status";
import type { FolderRecord } from "@/types/projects";
import type { SpaceBoardFilter } from "../space-api";
import { SpaceBoardFilterPopover } from "./space-board-filter-popover";

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
    (statusId: string, e: React.MouseEvent) => {
      e.stopPropagation();
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

  return (
    <div className="px-2 py-1 flex items-center shrink-0 select-none gap-2 bg-card border-b border-border shadow-sm z-10">
      {/* Status column toggles */}
      <div className="flex items-center gap-1 overflow-x-auto [&::-webkit-scrollbar]:hidden [-ms-overflow-style:none] [scrollbar-width:none] h-7 max-w-[40vw] shrink-0">
        {statuses.map((status) => {
          const isHidden = hiddenStatusIds.includes(status.id!);
          return (
            <div
              key={status.id}
              onClick={(e) => toggleStatusVisibility(status.id!, e)}
              onDoubleClick={() => isolateStatus(status.id!)}
              className={cn(
                "group flex items-center cursor-pointer shrink-0 transition-all duration-200 border border-transparent hover:bg-white/3 rounded-full",
                isHidden && "opacity-30 saturate-[0.2]",
              )}
            >
              <StatusBadge status={status} variant="outline" className="h-6 flex items-center pointer-events-none" />
            </div>
          );
        })}

        {/* Unclassified chip */}
        <div
          onClick={onToggleUnclassified}
          className={cn(
            "group flex items-center cursor-pointer shrink-0 transition-all duration-200 border border-transparent hover:bg-white/3 rounded-full",
            hideUnclassified && "opacity-30 saturate-[0.2]",
          )}
        >
          <span className="h-6 flex items-center px-2.5 text-[10px] font-semibold text-muted-foreground/70 border border-border/30 rounded-full pointer-events-none">
            Unclassified
          </span>
          <button
            onClick={(e) => { e.stopPropagation(); onToggleUnclassified(); }}
            className="w-0 opacity-0 group-hover:w-6 group-hover:opacity-100 flex items-center justify-center hover:bg-white/10 hover:text-foreground text-muted-foreground transition-all overflow-hidden h-6 rounded-r-full"
            title={hideUnclassified ? "Show unclassified" : "Hide unclassified"}
          >
            {hideUnclassified ? <Eye className="h-3 w-3 shrink-0" /> : <EyeOff className="h-3 w-3 shrink-0" />}
          </button>
        </div>
      </div>

      {hasEmptyStatuses && (
        <button
          onClick={onToggleHideEmpty}
          className={cn(
            "flex items-center justify-center h-6 w-6 rounded-full border transition-all cursor-pointer shrink-0",
            allEmptyHidden
              ? "bg-primary/10 text-primary border-primary/30 hover:bg-primary/20"
              : "bg-muted/40 text-muted-foreground border-border/30 hover:bg-muted/80 hover:text-foreground"
          )}
          title={allEmptyHidden ? "Show empty columns" : "Hide empty columns"}
        >
          {allEmptyHidden ? <Eye className="h-3 w-3" /> : <EyeOff className="h-3 w-3" />}
        </button>
      )}

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
        <input
          type="text"
          placeholder="Search..."
          value={searchInput}
          onChange={(e) => onSearchChange(e.target.value)}
          className="h-7 w-36 focus:w-48 bg-secondary/60 hover:bg-secondary/80 focus:bg-secondary/80 border border-transparent focus:border-primary/30 focus:ring-1 focus:ring-primary/20 rounded-md px-2.5 text-[11px] text-foreground/80 outline-none transition-all placeholder:text-muted-foreground/50 shadow-inner"
        />
        <SpaceBoardFilterPopover filter={filter} onChange={onFilterChange} folders={folders} />
      </div>
    </div>
  );
}
