import { useMemo } from "react";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { StatusBadge } from "@/components/status-badge";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useSelector } from "react-redux";
import { statusSelectors } from "@/store/entityStore";
import type { Status } from "@/types/status";

interface StatusSelectProps {
  value?: string;
  onChange: (statusId: string) => void;
  workflowId?: string;
  align?: "start" | "end" | "center";
  trigger?: React.ReactNode;
}

export function StatusSelect({
  value,
  onChange,
  workflowId,
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
    if (workflow?.statuses?.length) return workflow.statuses;
    if (!workflowId) return [];
    return allStatuses.filter((s: Status) => s.workflowId?.toLowerCase() === workflowId?.toLowerCase());
  }, [workflow, workflowId, allStatuses]);

  const currentStatus = useMemo(() => {
    return allStatuses.find((s: Status) => s.id === value || s.id === value) || null;
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
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        {trigger || (
          <div className="cursor-pointer">
            <StatusBadge status={currentStatus} className="text-[10px] font-bold" />
          </div>
        )}
      </DropdownMenuTrigger>
      <DropdownMenuContent align={align} className="w-44 p-0.5 bg-background/95 backdrop-blur-md border-border/40 shadow-2xl rounded-xl">
        <div className="px-1.5 py-1 border-b border-border/10 mb-0.5">
          <span className="text-[9px] font-black uppercase tracking-widest text-muted-foreground/40">Select Status</span>
        </div>
        <div className="max-h-48 overflow-y-auto space-y-1 p-0.5 no-scrollbar">
          {Object.entries(statusesByCategory).map(([category, cats]) => (
            <div key={category} className="space-y-0.5">
              <div className="px-1.5 py-0.5">
                <span className="text-[7px] font-black uppercase tracking-[0.1em] text-muted-foreground/30">{category}</span>
              </div>
              {cats.map((status: any) => (
                <DropdownMenuItem 
                  key={status.statusId || status.id}
                  onSelect={() => onChange(status.statusId || status.id)}
                  className="p-0.5 rounded-lg cursor-pointer transition-colors"
                >
                  <StatusBadge status={status} className="w-full justify-start border-none bg-transparent hover:bg-muted/20 text-[10px] py-0.5 px-1.5 h-auto" />
                </DropdownMenuItem>
              ))}
            </div>
          ))}
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
