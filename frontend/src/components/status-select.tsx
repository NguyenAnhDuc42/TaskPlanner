import * as React from "react";
import { useMemo } from "react";
import { StatusBadge } from "@/components/status-badge";
import { useSelector } from "react-redux";
import { statusSelectors, workflowSelectors } from "@/store/entityStore";
import type { Status } from "@/types/status";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuGroup,
  DropdownMenuItem,
} from "@/components/ui/dropdown-menu";
import { IconCheck } from "@tabler/icons-react";
import { cn } from "@/lib/utils";

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
}: Readonly<StatusSelectProps>) {
  const allWorkflows = useSelector(workflowSelectors.selectAll);

  const workflow = useMemo(() => {
    if (!workflowId) return null;
    return allWorkflows.find((w) =>
      w.id?.toLowerCase() === workflowId?.toLowerCase()
    );
  }, [workflowId, allWorkflows]);

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
    const grouped: Record<string, Status[]> = {};
    statuses.forEach((status: Status) => {
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
          <button type="button" className="cursor-pointer focus:outline-none bg-transparent border-none p-0">
            <StatusBadge status={currentStatus} variant="pill" />
          </button>
        )}
      </DropdownMenuTrigger>
      <DropdownMenuContent
        align={align}
        className="w-48 max-h-[300px] overflow-y-auto"
      >
        <DropdownMenuLabel>Status</DropdownMenuLabel>
        
        {Object.entries(statusesByCategory).map(([category, cats]) => (
          <React.Fragment key={category}>
            <DropdownMenuSeparator />
            <DropdownMenuGroup>
              <DropdownMenuLabel className="text-[9px] text-muted-foreground/60">{category}</DropdownMenuLabel>
              {cats.map((status: Status) => {
                const statusId = status.id;
                const isSelected = value?.toLowerCase() === statusId?.toLowerCase();
                return (
                  <DropdownMenuItem
                    key={statusId}
                    onSelect={() => {
                      if (!isSelected) onChange(statusId);
                    }}
                    className={cn("gap-2", isSelected && "bg-muted shadow-sm")}
                  >
                    <StatusBadge status={status} className="w-full justify-start border-none bg-transparent hover:bg-transparent text-[10px] p-0 h-auto" />
                    {isSelected && <IconCheck className="ml-auto size-4" />}
                  </DropdownMenuItem>
                );
              })}
            </DropdownMenuGroup>
          </React.Fragment>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
