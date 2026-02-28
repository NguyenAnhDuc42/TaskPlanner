import { Badge } from "@/components/ui/badge";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Plus, MoreHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";
import { TaskCard } from "./task-card";
import type { TaskDto } from "../../views-type";

interface BoardColumnProps {
  name: string;
  color: string;
  tasks: TaskDto[];
}

export function BoardColumn({ name, color, tasks }: BoardColumnProps) {
  return (
    <div className="w-80 flex-shrink-0 flex flex-col h-full bg-muted/10 rounded-xl border border-muted-foreground/10 shadow-sm overflow-hidden">
      <div className="p-3 bg-background/40 backdrop-blur-md flex items-center justify-between sticky top-0 z-10 border-b border-muted-foreground/5">
        <div className="flex items-center gap-2.5 overflow-hidden">
          <div
            className="w-2.5 h-2.5 rounded-full shadow-[0_0_8px_rgba(0,0,0,0.1)] flex-shrink-0"
            style={{ backgroundColor: color }}
          />
          <h3 className="font-bold text-xs truncate uppercase tracking-widest text-foreground/70">
            {name}
          </h3>
          <Badge
            variant="outline"
            className="px-1.5 h-4 text-[10px] border-none bg-muted/50 font-bold"
          >
            {tasks.length}
          </Badge>
        </div>
        <div className="flex items-center gap-0.5">
          <Button
            variant="ghost"
            size="icon"
            className="h-7 w-7 hover:bg-muted/50 rounded-full"
          >
            <Plus className="h-3.5 w-3.5" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className="h-7 w-7 hover:bg-muted/50 rounded-full"
          >
            <MoreHorizontal className="h-3.5 w-3.5 text-muted-foreground" />
          </Button>
        </div>
      </div>

      <ScrollArea className="flex-1">
        <div className="p-3 space-y-3">
          {tasks.map((task) => (
            <TaskCard key={task.id} task={task} />
          ))}

          <Button
            variant="ghost"
            className="w-full justify-start gap-2 h-10 text-muted-foreground hover:text-foreground hover:bg-muted/30 transition-all border border-dashed border-muted-foreground/20 rounded-lg text-xs font-medium"
          >
            <Plus className="h-3.5 w-3.5" />
            Add Task
          </Button>
        </div>
      </ScrollArea>
    </div>
  );
}
