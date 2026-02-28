import { Badge } from "@/components/ui/badge";
import { ListTable } from "./list-table";
import type { TaskDto, StatusDto } from "../../views-type";
import { Plus, MoreHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";

interface StatusSectionProps {
  status: StatusDto;
  tasks: TaskDto[];
  visibleCols: string[];
}

export function StatusSection({
  status,
  tasks,
  visibleCols,
}: StatusSectionProps) {
  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between px-2 group/header">
        <div className="flex items-center gap-3">
          <div
            className="w-2.5 h-2.5 rounded-full shadow-sm"
            style={{ backgroundColor: status.color }}
          />
          <h3 className="font-bold text-[12px] uppercase tracking-widest text-foreground/80">
            {status.name}
          </h3>
          <Badge
            variant="outline"
            className="bg-muted/50 text-[10px] px-1.5 py-0 border-none font-semibold text-muted-foreground"
          >
            {tasks.length}
          </Badge>
        </div>

        <div className="flex items-center gap-1 opacity-0 group-hover/header:opacity-100 transition-opacity">
          <Button variant="ghost" size="icon" className="h-7 w-7">
            <Plus className="h-3.5 w-3.5" />
          </Button>
          <Button variant="ghost" size="icon" className="h-7 w-7">
            <MoreHorizontal className="h-3.5 w-3.5" />
          </Button>
        </div>
      </div>

      <ListTable tasks={tasks} visibleCols={visibleCols} />

      <Button
        variant="ghost"
        className="w-full justify-start gap-2 h-9 text-muted-foreground hover:text-foreground hover:bg-muted/30 transition-all text-xs border-t border-transparent hover:border-muted px-4"
      >
        <Plus className="h-3.5 w-3.5" />
        Add Task
      </Button>
    </div>
  );
}
