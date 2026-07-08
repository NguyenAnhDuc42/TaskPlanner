import * as React from "react";
import { useMemo } from "react";
import { observer } from "mobx-react-lite";
import { StatusBadge } from "@/components/status-badge";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
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
  onChange: (statusId: string | null) => void;
  spaceId?: string;
  statuses?: Status[];
  align?: "start" | "end" | "center";
  trigger?: React.ReactNode;
}

export const StatusSelect = observer(function StatusSelect({
  value,
  onChange,
  spaceId,
  statuses: customStatuses,
  align = "start",
  trigger,
}: Readonly<StatusSelectProps>) {
  const rootStore = useWorkspaceRootStore();
  const allStatuses = rootStore.statusStore.all;

  const statuses = useMemo(() => {
    if (customStatuses) return customStatuses;
    if (spaceId) {
      const filtered = allStatuses.filter((s: Status) => s.spaceId?.toLowerCase() === spaceId.toLowerCase());
      if (filtered.length > 0) return filtered;
    }
    return allStatuses;
  }, [customStatuses, spaceId, allStatuses]);

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
            <StatusBadge status={currentStatus} variant="outline" />
          </button>
        )}
      </DropdownMenuTrigger>
      <DropdownMenuContent
        align={align}
        className="w-48 max-h-[300px] overflow-y-auto [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/40 [&::-webkit-scrollbar-track]:bg-transparent"
        onClick={(e) => e.stopPropagation()}
        onPointerDown={(e) => e.stopPropagation()}
      >
        <DropdownMenuLabel>Status</DropdownMenuLabel>
        <DropdownMenuItem
          onSelect={() => { if (value) onChange(null); }}
          className={cn("gap-2 text-muted-foreground/70", !value && "bg-muted shadow-sm")}
        >
          <StatusBadge status={null} variant="outline" className="w-full justify-start pointer-events-none" />
          {!value && <IconCheck className="ml-auto size-4" />}
        </DropdownMenuItem>

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
                    onSelect={() => { if (!isSelected) onChange(statusId); }}
                    className={cn("gap-2", isSelected && "bg-muted shadow-sm")}
                  >
                    <StatusBadge variant="outline" status={status} className="w-full justify-start pointer-events-none" />
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
});
