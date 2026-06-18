import { SlidersHorizontal, Plus, EyeOff, Eye } from "lucide-react";
import { StatusBadge } from "@/components/status-badge";
import { useCallback } from "react";
import { cn } from "@/lib/utils";
import type { Status } from "@/types/status";

interface SpaceFilterBarProps {
  statuses: Status[];
  hiddenStatusIds: string[];
  setHiddenStatusIds: React.Dispatch<React.SetStateAction<string[]>>;
  onWorkflowOpen?: () => void;
}

export function SpaceFilterBar({
  statuses,
  hiddenStatusIds,
  setHiddenStatusIds,
  onWorkflowOpen,
}: Readonly<SpaceFilterBarProps>) {

  const toggleStatusVisibility = useCallback((statusId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    setHiddenStatusIds((prev) =>
      prev.includes(statusId)
        ? prev.filter((id) => id !== statusId)
        : [...prev, statusId]
    );
  }, [setHiddenStatusIds]);

  const isolateStatus = useCallback((statusId: string) => {
    setHiddenStatusIds((prev) => {
      const allOtherIds = statuses.map(s => s.id!).filter(id => id !== statusId);
      const isIsolated = prev.length === allOtherIds.length && allOtherIds.every(id => prev.includes(id));
      
      if (isIsolated) {
        return []; // show all
      } else {
        return allOtherIds; // hide all others
      }
    });
  }, [statuses, setHiddenStatusIds]);

  return (
    <div className="px-2 py-2 flex items-center shrink-0 select-none gap-1 bg-background/20 backdrop-blur-sm">

      {/* Max width and scrollable container for statuses */}
      <div className="flex items-center gap-1 overflow-x-auto [&::-webkit-scrollbar]:hidden [-ms-overflow-style:none] [scrollbar-width:none] h-7 max-w-[50vw]">
        {statuses.map((status) => {
          const isHidden = hiddenStatusIds.includes(status.id!);
          return (
            <div
              key={status.id}
              onClick={() => isolateStatus(status.id!)}
              className={cn(
                "group flex items-center cursor-pointer shrink-0 transition-all duration-200 border border-transparent hover:bg-white/[0.03] rounded-full",
                isHidden ? "opacity-30 saturate-[0.2]" : ""
              )}
            >
              <div className="flex items-center">
                <StatusBadge status={status} variant="outline" className="h-6 flex items-center pointer-events-none" />
              </div>
              
              {/* Eye Icon Hover */}
              <button
                onClick={(e) => toggleStatusVisibility(status.id!, e)}
                className="w-0 opacity-0 group-hover:w-6 group-hover:opacity-100 flex items-center justify-center hover:bg-white/10 hover:text-foreground text-muted-foreground transition-all overflow-hidden h-6 rounded-r-full"
                title={isHidden ? "Show status" : "Hide status"}
              >
                {isHidden ? <Eye className="h-3 w-3 shrink-0" /> : <EyeOff className="h-3 w-3 shrink-0" />}
              </button>
            </div>
          );
        })}
      </div>
      {onWorkflowOpen && (
          <button
            className="flex items-center justify-center h-6 w-6 rounded-full bg-muted/40 text-muted-foreground hover:bg-muted/80 hover:text-foreground border border-border/30 transition-all cursor-pointer shrink-0 ml-1"
            onClick={onWorkflowOpen}
            title="Add Status (Workflow)"
          >
            <Plus className="h-3 w-3" />
          </button>
        )}

      <button
        className="ml-auto flex items-center h-6 gap-1 px-2 rounded-md bg-muted/40 text-[10px] text-muted-foreground font-semibold hover:bg-muted/80 hover:text-foreground border border-border/30 transition-all cursor-pointer shrink-0"
        onClick={() => {
          console.log("Filter clicked");
        }}
      >
        <SlidersHorizontal className="h-3 w-3 opacity-70" />
        <span>Filter</span>
      </button>
    </div>
  );
}
