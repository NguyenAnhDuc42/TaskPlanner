import { Trash2, X } from "lucide-react";
import { useBatchUpdateFolderTasks, type BatchUpdateFolderTaskValue } from "../folder-api";
import { PriorityBadge } from "@/components/priority-badge";
import { StatusBadge } from "@/components/status-badge";
import { useState } from "react";
import { StatusSelect } from "@/components/status-select";
import { PrioritySelect } from "@/components/priority-select";
import { DateSelect } from "@/components/date-select";


import type { Status } from "@/types/status";

type PendingUpdates = Pick<BatchUpdateFolderTaskValue, "statusId" | "priority" | "startDate" | "dueDate" | "clearStartDate" | "clearDueDate">;

interface FolderTaskBatchBarProps {
  folderId: string;
  checkedTaskIds: Set<string>;
  onClear: () => void;
  statuses: Status[];
}

export function FolderTaskBatchBar({ folderId, checkedTaskIds, onClear, statuses }: FolderTaskBatchBarProps) {
  const batchUpdate = useBatchUpdateFolderTasks(folderId);

  const [pendingUpdates, setPendingUpdates] = useState<PendingUpdates>({});

  const handleDelete = () => {
    batchUpdate.mutate(
      Array.from(checkedTaskIds).map(id => ({ id, isDeleted: true }))
    );
    onClear();
  };

  const updateLocal = (updates: Partial<PendingUpdates>) => {
    setPendingUpdates((prev) => ({ ...prev, ...updates }));
  };

  const applyChanges = () => {
    if (Object.keys(pendingUpdates).length === 0) return;
    batchUpdate.mutate(
      Array.from(checkedTaskIds).map(id => ({ id, ...pendingUpdates }))
    );
    setPendingUpdates({});
    onClear();
  };

  const pendingCount = Object.keys(pendingUpdates).length;

  return (
    <div className="absolute bottom-8 left-1/2 -translate-x-1/2 bg-popover text-popover-foreground border border-border shadow-xl rounded-md px-3 py-1.5 flex items-center gap-3 z-50 animate-in slide-in-from-bottom-4 fade-in duration-200">
      <div className="flex items-center gap-2 pl-1">
        <span className="flex h-5 w-5 items-center justify-center rounded-md bg-primary/20 text-primary text-[10px] font-bold">
          {checkedTaskIds.size}
        </span>
        <span className="text-[11px] font-medium text-foreground whitespace-nowrap pr-1">Tasks selected</span>
      </div>

      <div className="w-[1px] h-4 bg-border" />

      <div className="flex items-center gap-2 pr-10">
        {/* Status */}
        <StatusSelect
          value={pendingUpdates.statusId || undefined}
          onChange={(statusId) => updateLocal({ statusId })}
          statuses={statuses}
          align="center"
          trigger={
            <button type="button" className="cursor-pointer hover:opacity-80 transition-opacity">
              <StatusBadge
                status={statuses.find(s => s.id?.toLowerCase() === pendingUpdates.statusId?.toLowerCase())}
                variant="outline"
              />
            </button>
          }
        />

        {/* Priority */}
        <PrioritySelect
          value={pendingUpdates.priority ?? undefined}
          onChange={(priority) => updateLocal({ priority })}
          align="center"
          trigger={
            <button type="button" className="cursor-pointer hover:opacity-80 transition-opacity">
              <PriorityBadge priority={pendingUpdates.priority ?? undefined} />
            </button>
          }
        />

        {/* Dates */}
        <DateSelect
          startDate={pendingUpdates.startDate}
          dueDate={pendingUpdates.dueDate}
          onStartDateChange={(date) => updateLocal({ startDate: date?.toISOString(), clearStartDate: !date })}
          onDueDateChange={(date) => updateLocal({ dueDate: date?.toISOString(), clearDueDate: !date })}
          align="center"
          size="sm"
          triggerClassName={`h-5 px-2 text-[10px] font-semibold rounded-md border transition-all cursor-pointer ${
            pendingUpdates.startDate || pendingUpdates.dueDate || pendingUpdates.clearStartDate || pendingUpdates.clearDueDate
              ? "border-border/40 bg-primary/10 text-primary hover:bg-primary/20"
              : "border-border/30 bg-muted/30 text-muted-foreground hover:bg-muted/60"
          }`}
        />
      </div>

        <div className="w-[1px] h-3 bg-border mx-1" />

        {pendingCount > 0 && (
          <button
            className="h-7 px-3 bg-gradient-to-r from-primary to-primary/80 hover:from-primary/90 hover:to-primary/70 text-primary-foreground rounded-md text-[11px] font-bold tracking-wide shadow-md transition-all hover:scale-105 active:scale-95"
            onClick={applyChanges}
          >
            Apply
          </button>
        )}

        <button
          onClick={handleDelete}
          className="h-7 w-7 rounded-md flex items-center justify-center hover:bg-destructive/10 text-destructive transition-colors"
          title="Delete"
        >
          <Trash2 className="h-3.5 w-3.5" />
        </button>

      <div className="w-[1px] h-4 bg-border" />

      <button
        onClick={onClear}
        className="h-7 w-7 rounded-md flex items-center justify-center hover:bg-muted text-muted-foreground transition-colors"
      >
        <X className="h-4 w-4" />
      </button>
    </div>
  );
}
