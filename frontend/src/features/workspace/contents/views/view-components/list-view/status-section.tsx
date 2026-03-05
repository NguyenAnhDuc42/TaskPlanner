import { Badge } from "@/components/ui/badge";
import { ListTable } from "./list-table";
import type { TaskDto, StatusDto } from "../../views-type";
import { Plus, MoreHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";
import { InlineCreateTask } from "../../../tasks/inline-create-task";

interface StatusSectionProps {
  status: StatusDto;
  tasks: TaskDto[];
  visibleCols: string[];
  workspaceId: string;
  layerId: string;
  layerType: "ProjectSpace" | "ProjectFolder" | "ProjectList";
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
  return (
    <div
      className="space-y-4 p-2 rounded-xl transition-all border-2"
      style={{
        borderColor: `${status.color}20`,
        backgroundColor: `${status.color}05`,
      }}
    >
      <div className="flex items-center justify-between px-2 group/header">
        <div className="flex items-center gap-3">
          <div
            className="w-2.5 h-2.5 rounded-full shadow-sm"
            style={{ backgroundColor: status.color }}
          />
          <h3 className="font-bold text-[11px] uppercase tracking-widest text-foreground/70">
            {status.name}
          </h3>
          <Badge
            variant="outline"
            className="bg-background/50 text-[10px] px-1.5 py-0 border-none font-bold text-muted-foreground/60 shadow-none"
          >
            {tasks.length}
          </Badge>
        </div>

        <div className="flex items-center gap-1 opacity-0 group-hover/header:opacity-100 transition-opacity">
          <Button
            variant="ghost"
            size="icon"
            className="h-7 w-7 hover:bg-background/80"
          >
            <Plus className="h-3.5 w-3.5" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className="h-7 w-7 hover:bg-background/80"
          >
            <MoreHorizontal className="h-3.5 w-3.5" />
          </Button>
        </div>
      </div>

      <div className="bg-background/40 rounded-lg overflow-hidden border border-muted/20">
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
