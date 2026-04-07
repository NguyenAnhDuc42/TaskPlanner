import { ListTable } from "./list-table";
import type { TaskDto, StatusDto } from "../../views-type";
import { Plus, MoreHorizontal } from "lucide-react";
import type { EntityLayerType } from "@/types/entity-layer-type";

interface StatusSectionProps {
  status: StatusDto;
  tasks: TaskDto[];
  visibleCols: string[];
  workspaceId: string;
  layerId: string;
  layerType: EntityLayerType;
  listId?: string;
  isInlineOpen: boolean;
  onInlineOpenChange: (open: boolean) => void;
  onTaskClick?: (task: TaskDto) => void;
}

export function StatusSection({
  status,
  tasks,
  visibleCols,
  onInlineOpenChange,
  onTaskClick,
}: StatusSectionProps) {
  const statusColor = status.color || "var(--primary)";

  return (
    <div className="space-y-2 rounded-xl border border-border bg-background shadow-sm overflow-hidden">
      {/* Status Header */}
      <div className="flex items-center justify-between px-4 py-2 border-b border-border group/header">
        <div className="flex items-center gap-3">
          <div
            className="w-1.5 h-4 rounded-full"
            style={{ backgroundColor: statusColor }}
          />
          <div className="flex flex-col gap-0.5">
            <h3 className="font-black text-[10px] uppercase tracking-[0.2em] text-foreground/80">
              {status.name}
            </h3>
            <div className="text-[9px] font-bold text-muted-foreground/40 uppercase tracking-widest leading-none">
              {tasks.length} tasks
            </div>
          </div>
        </div>

        <div className="flex items-center gap-1 opacity-0 group-hover/header:opacity-100 transition-opacity">
          <div
            className="p-1.5 rounded-md hover:bg-muted cursor-pointer transition-colors"
            onClick={() => onInlineOpenChange(true)}
          >
            <Plus className="h-3.5 w-3.5 text-muted-foreground/60 hover:text-foreground transition-colors" />
          </div>
          <div className="p-1.5 rounded-md hover:bg-muted cursor-pointer transition-colors">
            <MoreHorizontal className="h-3.5 w-3.5 text-muted-foreground/60 hover:text-foreground transition-colors" />
          </div>
        </div>
      </div>

      {/* Task Table */}
      <ListTable
        tasks={tasks}
        visibleCols={visibleCols}
        onTaskClick={onTaskClick}
      />
    </div>
  );
}
