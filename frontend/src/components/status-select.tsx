import { useMemo } from "react";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { StatusBadge } from "@/components/status-badge";
import { useSelector } from "react-redux";
import { statusSelectors } from "@/store/entityStore";
import type { Status } from "@/types/status";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";

interface StatusSelectProps {
  value?: string;
  onChange: (statusId: string) => void;
  workflowId?: string;
  statuses?: Status[];
  align?: "start" | "end" | "center";
  trigger?: React.ReactNode;
}

export function StatusSelect({
  value,
  onChange,
  workflowId,
  statuses: customStatuses,
  align = "start",
  trigger,
}: StatusSelectProps) {
  const { registry } = useWorkspace();

  const workflow = useMemo(() => {
    if (!workflowId) return null;
    return registry.workflows.find((w: any) => 
      w.id?.toLowerCase() === workflowId?.toLowerCase()
    );
  }, [workflowId, registry.workflows]);

  const allStatuses = useSelector(statusSelectors.selectAll);
  const statuses = useMemo(() => {
    if (customStatuses) return customStatuses;
    if (workflow?.statuses?.length) return workflow.statuses;
    if (workflowId) {
      const filtered = allStatuses.filter((s: Status) => s.workflowId?.toLowerCase() === workflowId?.toLowerCase());
      if (filtered.length > 0) return filtered;
    }
    return allStatuses;
  }, [customStatuses, workflow, workflowId, allStatuses]);

  const currentStatus = useMemo(() => {
    return allStatuses.find((s: Status) => s.id?.toLowerCase() === value?.toLowerCase()) || null;
  }, [value, allStatuses]);

  const statusesByCategory = useMemo(() => {
    const grouped: Record<string, any[]> = {};
    statuses.forEach((status: any) => {
      const cat = status.category || "Other";
      if (!grouped[cat]) grouped[cat] = [];
      grouped[cat].push(status);
    });
    return grouped;
  }, [statuses]);

  return (
    <Popover>
      <PopoverTrigger asChild>
        {trigger || (
          <button type="button" className="cursor-pointer focus:outline-none">
            <StatusBadge status={currentStatus} className="text-[10px] font-bold" />
          </button>
        )}
      </PopoverTrigger>
      <PopoverContent
        align={align}
        className="w-44 p-1 bg-popover border border-border shadow-md rounded-md"
        onFocusOutside={(e) => e.preventDefault()}
      >
        <div className="px-1.5 py-0.5 border-b border-border/10 mb-1">
          <span className="text-[8px] font-black uppercase tracking-wider text-muted-foreground/50">Select Status</span>
        </div>
        <div className="max-h-48 overflow-y-auto space-y-1 p-0.5 no-scrollbar flex flex-col gap-0.5">
          {Object.entries(statusesByCategory).map(([category, cats]) => (
            <div key={category} className="space-y-0.5 flex flex-col">
              <div className="px-1.5 py-0.5">
                <span className="text-[7px] font-black uppercase tracking-[0.1em] text-muted-foreground/30">{category}</span>
              </div>
              {cats.map((status: any) => (
                <button
                  key={status.statusId || status.id}
                  type="button"
                  onClick={() => onChange(status.statusId || status.id)}
                  className="px-1.5 py-1 text-xs text-left rounded-sm hover:bg-muted transition-colors flex items-center w-full"
                >
                  <StatusBadge status={status} className="w-full justify-start border-none bg-transparent hover:bg-transparent text-[10px] p-0 h-auto" />
                </button>
              ))}
            </div>
          ))}
        </div>
      </PopoverContent>
    </Popover>
  );
}
