import { ListTable } from "./list-table";
import type { TaskDto, StatusDto } from "../../views-type";
import { Plus, MoreHorizontal } from "lucide-react";
import { InlineCreateTask } from "../../../tasks/inline-create-task";
import type { EntityLayerType } from "@/types/relationship-type";

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
  workspaceId,
  layerId,
  layerType,
  listId,
  isInlineOpen,
  onInlineOpenChange,
  onTaskClick,
}: StatusSectionProps) {
  const statusColor = status.color || "var(--primary)";

  return (
    <div
      className="space-y-4 p-4 rounded-[2rem] transition-all border border-white/5 bg-white/[0.02] backdrop-blur-sm shadow-xl relative overflow-hidden"
    >
      {/* Status Header */}
      <div className="flex items-center justify-between px-2 group/header z-10 relative">
        <div className="flex items-center gap-4">
          <div
            className="w-1.5 h-6 rounded-full shadow-[0_0_8px_currentColor]"
            style={{ backgroundColor: statusColor, color: statusColor }}
          />
          <div className="flex flex-col gap-0.5">
            <h3 className="font-black text-[10px] uppercase tracking-[0.25em] text-foreground/90">
              {status.name}
            </h3>
            <div className="text-[9px] font-bold text-muted-foreground/30 uppercase tracking-widest flex items-center gap-1.5 leading-none">
              <span style={{ color: statusColor }}>{tasks.length}</span> objectives discovered
            </div>
          </div>
        </div>

        <div className="flex items-center gap-3 opacity-0 group-hover/header:opacity-100 transition-all duration-300 transform group-hover/header:translate-x-0 translate-x-2">
          <div
            className="p-1.5 rounded-md hover:bg-white/10 cursor-pointer transition-colors group/btn"
            onClick={() => onInlineOpenChange(true)}
          >
            <Plus className="h-3.5 w-3.5 text-muted-foreground/60 group-hover/btn:text-foreground transition-colors" />
          </div>
          <div
            className="p-1.5 rounded-md hover:bg-white/10 cursor-pointer transition-colors group/btn"
          >
            <MoreHorizontal className="h-3.5 w-3.5 text-muted-foreground/60 group-hover/btn:text-foreground transition-colors" />
          </div>
        </div>
      </div>

      {/* Task Table Container */}
      <div className="rounded-2xl overflow-hidden border border-white/5 bg-black/20 z-10 relative">
        <ListTable
          tasks={tasks}
          visibleCols={visibleCols}
          onTaskClick={onTaskClick}
        />
      </div>

      <InlineCreateTask
        listId={listId}
        statusId={status.id}
        workspaceId={workspaceId}
        layerId={layerId}
        layerType={layerType}
        isOpen={isInlineOpen}
        onOpenChange={onInlineOpenChange}
      />
    </div>
  );
}
