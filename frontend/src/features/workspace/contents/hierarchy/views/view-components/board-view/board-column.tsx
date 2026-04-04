import { EntityLayerType } from "@/types/relationship-type";
import { ScrollArea } from "@/components/ui/scroll-area";
import { MoreHorizontal, Plus } from "lucide-react";
import { TaskCard } from "./task-card";
import type { TaskDto } from "../../views-type";

interface BoardColumnProps {
  name: string;
  color: string;
  tasks: TaskDto[];
  statusId?: string;
  workspaceId: string;
  layerId: string;
  layerType: EntityLayerType;
  listId?: string;
  isInlineOpen: boolean;
  onInlineOpenChange: (open: boolean) => void;
  onTaskClick?: (task: TaskDto) => void;
}

export function BoardColumn({
  name,
  color,
  tasks,
  isInlineOpen,
  onInlineOpenChange,
  onTaskClick,
}: BoardColumnProps) {
  const columnColor = color || "var(--primary)";

  return (
    <div className="w-[300px] flex-shrink-0 flex flex-col rounded-xl border border-border bg-background shadow-sm overflow-hidden">
      {/* Header */}
      <div className="px-4 py-3 flex items-center justify-between border-b border-border group/col">
        <div className="flex items-center gap-3 overflow-hidden">
          <div
            className="w-2 h-2 rounded-full flex-shrink-0"
            style={{ backgroundColor: columnColor }}
          />
          <h3 className="font-black text-[10px] truncate uppercase tracking-[0.2em] text-foreground/70">
            {name}
          </h3>
          <div className="px-1.5 py-0.5 rounded-sm bg-muted text-[9px] font-bold text-muted-foreground/50">
            {tasks.length}
          </div>
        </div>

        <div className="flex items-center gap-1 opacity-0 group-hover/col:opacity-100 transition-opacity">
          <div
            className="p-1.5 rounded-md hover:bg-muted cursor-pointer transition-colors"
            onClick={() => onInlineOpenChange(true)}
          >
            <Plus className="h-3.5 w-3.5 text-muted-foreground/50 hover:text-foreground transition-colors" />
          </div>
          <div className="p-1.5 rounded-md hover:bg-muted cursor-pointer transition-colors">
            <MoreHorizontal className="h-3.5 w-3.5 text-muted-foreground/50 hover:text-foreground transition-colors" />
          </div>
        </div>
      </div>

      <ScrollArea className="flex-1">
        <div className="p-3 space-y-2">
          {tasks.map((task) => (
            <TaskCard key={task.id} task={task} onClick={onTaskClick} />
          ))}

          {tasks.length === 0 && !isInlineOpen && (
            <div className="py-10 flex flex-col items-center justify-center gap-2 text-muted-foreground/30">
              <span className="text-[9px] font-black uppercase tracking-[0.2em]">Empty</span>
            </div>
          )}
        </div>
      </ScrollArea>
    </div>
  );
}
