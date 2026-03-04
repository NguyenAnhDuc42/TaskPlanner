import { Badge } from "@/components/ui/badge";
import { ScrollArea } from "@/components/ui/scroll-area";
import { MoreHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";
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
  layerType: "ProjectSpace" | "ProjectFolder" | "ProjectList";
  listId?: string;
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
  onTaskClick,
}: BoardColumnProps) {
  return (
    <div
      className="w-80 flex-shrink-0 flex flex-col h-full rounded-2xl border-2 overflow-hidden transition-all"
      style={{
        borderColor: `${color}25`,
        backgroundColor: `${color}08`,
      }}
    >
      <div className="p-3 bg-background/40 backdrop-blur-md flex items-center justify-between sticky top-0 z-10 border-b border-muted/10">
        <div className="flex items-center gap-2.5 overflow-hidden">
          <div
            className="w-2.5 h-2.5 rounded-full shadow-sm flex-shrink-0"
            style={{ backgroundColor: color }}
          />
          <h3 className="font-bold text-[11px] truncate uppercase tracking-widest text-foreground/70">
            {name}
          </h3>
          <Badge
            variant="outline"
            className="px-1.5 h-4 text-[10px] border-none bg-background/50 font-black text-muted-foreground/60 shadow-none"
          >
            {tasks.length}
          </Badge>
        </div>
        <div className="flex items-center gap-0.5">
          <Button
            variant="ghost"
            size="icon"
            className="h-7 w-7 hover:bg-background/80 rounded-full"
          >
            <MoreHorizontal className="h-3.5 w-3.5 text-muted-foreground/60" />
          </Button>
        </div>
      </div>

      <ScrollArea className="flex-1">
        <div className="p-3 space-y-3">
          {tasks.map((task) => (
            <TaskCard key={task.id} task={task} onClick={onTaskClick} />
          ))}

          <InlineCreateTask
            listId={listId}
            statusId={statusId}
            workspaceId={workspaceId}
            layerId={layerId}
            layerType={layerType}
          />
        </div>
      </ScrollArea>
    </div>
  );
}
