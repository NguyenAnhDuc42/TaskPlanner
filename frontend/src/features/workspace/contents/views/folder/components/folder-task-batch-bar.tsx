import { CircleDashed, Flag, Calendar, Trash2, X } from "lucide-react";
import { useBatchUpdateFolderTasks, type BatchUpdateFolderTaskValue } from "../folder-api";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { PopoverFormWrapper } from "@/components/popover-wrapper";
import { Priority } from "@/types/priority";
import { PriorityBadge } from "@/components/priority-badge";
import { StatusBadge } from "@/components/status-badge";
import { Input } from "@/components/ui/input";
import { useState } from "react";


import type { Status } from "@/types/status";

type PendingUpdates = Pick<BatchUpdateFolderTaskValue, "statusId" | "priority" | "startDate" | "dueDate">;

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
    <div className="absolute bottom-8 left-1/2 -translate-x-1/2 bg-background/80 backdrop-blur-md border border-border shadow-xl rounded-md px-3 py-1.5 flex items-center gap-3 z-50 animate-in slide-in-from-bottom-4 fade-in duration-200">
      <div className="flex items-center gap-2 pl-1">
        <span className="flex h-5 w-5 items-center justify-center rounded-md bg-primary/20 text-primary text-[10px] font-bold">
          {checkedTaskIds.size}
        </span>
        <span className="text-[11px] font-medium text-foreground whitespace-nowrap pr-1">Tasks selected</span>
      </div>

      <div className="w-[1px] h-4 bg-border" />

      <div className="flex items-center gap-1">
        {/* Status */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button
              className={`h-7 px-2 min-w-[28px] rounded-md flex items-center justify-center transition-colors ${pendingUpdates.statusId ? "bg-primary/10 hover:bg-primary/20" : "hover:bg-muted text-muted-foreground"}`}
              title="Change Status"
            >
              {pendingUpdates.statusId ? (
                <StatusBadge status={statuses.find(s => s.id?.toLowerCase() === pendingUpdates.statusId?.toLowerCase())} showIcon={false} />
              ) : (
                <CircleDashed className="h-3.5 w-3.5" />
              )}
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="center" sideOffset={14}>
            {statuses.map(s => (
              <DropdownMenuItem key={s.id} onClick={() => updateLocal({ statusId: s.id })}>
                <div className="flex items-center gap-2 text-xs">
                  <div className="w-2 h-2 rounded-full" style={{ backgroundColor: s.color }} />
                  {s.name}
                </div>
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>

        {/* Priority */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button
              className={`h-7 px-2 min-w-[28px] rounded-md flex items-center justify-center transition-colors ${pendingUpdates.priority ? "bg-primary/10 hover:bg-primary/20" : "hover:bg-muted text-muted-foreground"}`}
              title="Change Priority"
            >
              {pendingUpdates.priority ? (
                <PriorityBadge priority={pendingUpdates.priority} showText={false} />
              ) : (
                <Flag className="h-3.5 w-3.5" />
              )}
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="center" sideOffset={14}>
            {Object.values(Priority).map(p => (
              <DropdownMenuItem key={p} onClick={() => updateLocal({ priority: p })}>
                <PriorityBadge priority={p} />
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>

        {/* Dates */}
        <PopoverFormWrapper
          trigger={
            <button
              className={`h-7 px-2 min-w-[28px] rounded-md flex items-center justify-center transition-colors gap-1.5 ${pendingUpdates.startDate || pendingUpdates.dueDate ? "bg-primary/10 text-primary hover:bg-primary/20" : "hover:bg-muted text-muted-foreground"}`}
              title="Set Dates"
            >
              <Calendar className="h-3.5 w-3.5" />
              {(pendingUpdates.startDate || pendingUpdates.dueDate) && (
                <span className="text-[10px] font-bold">
                  {pendingUpdates.startDate
                    ? new Date(pendingUpdates.startDate).toLocaleDateString(undefined, { month: "short", day: "numeric" })
                    : "..."}{" "}
                  -{" "}
                  {pendingUpdates.dueDate
                    ? new Date(pendingUpdates.dueDate).toLocaleDateString(undefined, { month: "short", day: "numeric" })
                    : "..."}
                </span>
              )}
            </button>
          }
          className="w-auto p-3"
        >
          <div className="flex flex-col gap-3">
            <div className="flex flex-col gap-1.5">
              <label className="text-[10px] font-bold uppercase text-muted-foreground">Start Date</label>
              <Input
                type="date"
                className="h-8 text-xs"
                defaultValue={pendingUpdates.startDate ? pendingUpdates.startDate.split("T")[0] : ""}
                onBlur={(e) => updateLocal({ startDate: e.target.value ? new Date(e.target.value).toISOString() : undefined })}
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <label className="text-[10px] font-bold uppercase text-muted-foreground">Due Date</label>
              <Input
                type="date"
                className="h-8 text-xs"
                defaultValue={pendingUpdates.dueDate ? pendingUpdates.dueDate.split("T")[0] : ""}
                onBlur={(e) => updateLocal({ dueDate: e.target.value ? new Date(e.target.value).toISOString() : undefined })}
              />
            </div>
          </div>
        </PopoverFormWrapper>

        <div className="w-[1px] h-3 bg-border mx-1" />

        {pendingCount > 0 && (
          <button
            className="h-7 px-3 bg-gradient-to-r from-primary to-primary/80 hover:from-primary/90 hover:to-primary/70 text-primary-foreground rounded-md text-[11px] font-bold tracking-wide shadow-md transition-all hover:scale-105 active:scale-95"
            onClick={applyChanges}
          >
            Apply Changes
          </button>
        )}

        <button
          onClick={handleDelete}
          className="h-7 w-7 rounded-md flex items-center justify-center hover:bg-destructive/10 text-destructive transition-colors"
          title="Delete"
        >
          <Trash2 className="h-3.5 w-3.5" />
        </button>
      </div>

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
