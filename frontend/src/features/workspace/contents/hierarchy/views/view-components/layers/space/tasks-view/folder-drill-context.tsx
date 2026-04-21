import { ScrollArea } from "@/components/ui/scroll-area";
import { Loader2 } from "lucide-react";
import { StatusGroup } from "../../../item-components/status-group";
import { TaskItem } from "../../../item-components/task-item";
import type { TaskItemDto, TaskViewData } from "../../../../views-type";
import { useViewData } from "../../../../views-api";


interface FolderDrillContextProps {
  folderId: string;
  folderName: string;
  onTaskSelect: (task: TaskItemDto) => void;
}

export function FolderDrillContext({
  folderId,
  folderName,
  onTaskSelect,
}: FolderDrillContextProps) {
  const { data, isLoading } = useViewData(folderId);

  if (isLoading) {
    return (
      <div className="flex-1 flex items-center justify-center">
        <Loader2 className="h-6 w-6 animate-spin text-primary/40" />
      </div>
    );
  }

  const viewData = data?.data as TaskViewData | undefined;

  return (
    <div className="flex-1 flex flex-col h-full bg-muted/5">
      <div className="h-14 px-6 flex flex-col justify-center border-b border-border/50 bg-background/50 backdrop-blur-md">
        <span className="text-[9px] font-black uppercase tracking-[0.2em] text-muted-foreground/30 leading-none mb-1">
          Folder Exploration
        </span>
        <span className="text-[13px] font-black text-foreground/80 tracking-tight italic">
          {folderName}
        </span>
      </div>

      <ScrollArea className="flex-1">
        <div className="p-6 space-y-8">
           {viewData?.groups?.map((group) => (
             <StatusGroup
               key={group.statusId}
               statusName={group.statusName}
               color={group.color}
               totalCount={group.tasks.length}
             >
                {group.tasks.map((task) => (
                  <TaskItem
                    key={task.id}
                    task={task}
                    onClick={onTaskSelect}
                  />
                ))}
             </StatusGroup>
           ))}

           {(!viewData?.groups || viewData.groups.length === 0) && (
              <div className="py-20 flex flex-col items-center justify-center gap-2">
                 <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/10">
                    No Tasks In Folder
                 </span>
              </div>
           )}
        </div>
      </ScrollArea>
    </div>
  );
}
