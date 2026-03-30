import { EntityLayerType } from "@/types/relationship-type";
import { ScrollArea } from "@/components/ui/scroll-area";
import { MoreHorizontal, Plus } from "lucide-react";
import { TaskCard } from "./task-card";
import type { TaskDto } from "../../views-type";
import { InlineCreateTask } from "../../../tasks/inline-create-task";

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
  statusId,
  workspaceId,
  layerId,
  layerType,
  listId,
  isInlineOpen,
  onInlineOpenChange,
  onTaskClick,
}: BoardColumnProps) {
  const columnColor = color || "var(--primary)";

  return (
    <div
      className="w-[340px] flex-shrink-0 flex flex-col h-full rounded-[2.5rem] border border-white/5 bg-white/[0.01] backdrop-blur-sm overflow-hidden transition-all duration-500 shadow-2xl relative group/column"
    >
      {/* Column Glow Accent */}
      <div 
        className="absolute top-0 left-0 right-0 h-1 blur-xl opacity-20 group-hover/column:opacity-40 transition-opacity"
        style={{ backgroundColor: columnColor }}
      />

      <div className="p-5 flex items-center justify-between sticky top-0 z-10 bg-white/[0.02] border-b border-white/5">
        <div className="flex items-center gap-4 overflow-hidden">
          <div
            className="w-1.5 h-1.5 rounded-full shadow-[0_0_8px_currentColor] flex-shrink-0"
            style={{ backgroundColor: columnColor, color: columnColor }}
          />
          <h3 className="font-black text-[10px] truncate uppercase tracking-[0.2em] text-foreground/80 group-hover/column:text-foreground transition-colors">
            {name}
          </h3>
          <div className="px-2 py-0.5 rounded-md bg-white/5 text-[9px] font-black text-muted-foreground/40 uppercase tracking-widest">
            {tasks.length}
          </div>
        </div>
        
        <div className="flex items-center gap-1 opacity-0 group-hover/column:opacity-100 transition-all duration-300 transform translate-x-2 group-hover/column:translate-x-0">
          <div
            className="p-2 rounded-lg hover:bg-white/10 cursor-pointer transition-colors group/btn"
            onClick={() => onInlineOpenChange(true)}
          >
            <Plus className="h-4 w-4 text-muted-foreground/40 group-hover/btn:text-primary transition-colors" />
          </div>
          <div
            className="p-2 rounded-lg hover:bg-white/10 cursor-pointer transition-colors group/btn"
          >
            <MoreHorizontal className="h-4 w-4 text-muted-foreground/40 group-hover/btn:text-foreground transition-colors" />
          </div>
        </div>
      </div>

      <ScrollArea className="flex-1">
        <div className="p-4 space-y-4">
          {tasks.map((task) => (
            <TaskCard key={task.id} task={task} onClick={onTaskClick} />
          ))}

          <InlineCreateTask
            listId={listId}
            statusId={statusId}
            workspaceId={workspaceId}
            layerId={layerId}
            layerType={layerType}
            isOpen={isInlineOpen}
            onOpenChange={onInlineOpenChange}
          />
          
          {tasks.length === 0 && !isInlineOpen && (
            <div className="py-12 flex flex-col items-center justify-center gap-3 opacity-20">
              <div className="w-10 h-[1px] bg-white opacity-20" />
              <span className="text-[9px] font-black uppercase tracking-[0.3em]">Sector Empty</span>
            </div>
          )}
        </div>
      </ScrollArea>
    </div>
  );
}
