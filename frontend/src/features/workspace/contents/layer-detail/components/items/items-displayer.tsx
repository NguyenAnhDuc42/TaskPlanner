import { useMemo } from "react";
import { useNavigate, useParams } from "@tanstack/react-router";
import { cn } from "@/lib/utils";
import { format } from "date-fns";
import { Hash, Layers, Clock, User, Folder as FolderIcon } from "lucide-react";

import { StatusGroup } from "./status-group";
import { TaskItem } from "./task-item";
import { FolderItem } from "./folder-item";
import type { ItemsViewMode, TaskViewData } from "../../layer-detail-types";

interface ItemsDisplayerProps {
  viewData: TaskViewData;
  viewMode: ItemsViewMode;
}

interface StatusGroupData {
  statusId: string;
  statusName: string;
  color: string;
  tasks: any[];
  folders: any[];
}

export function ItemsDisplayer({ viewData, viewMode }: ItemsDisplayerProps) {
  const navigate = useNavigate();
  const { workspaceId } = useParams({ strict: false }) as any;

  const groups = useMemo<StatusGroupData[]>(() => {
    if (!viewData) return [];
    const { tasks = [], statuses = [], folders = [] } = viewData;

    return statuses.map((status: any) => ({
      statusId: status.statusId,
      statusName: status.name,
      color: status.color,
      tasks: tasks.filter((t: any) => t.statusId === status.statusId),
      folders: folders.filter((f: any) => f.statusId === status.statusId),
    }));
  }, [viewData]);

  const handleFolderClick = (folderId: string) => {
    navigate({ to: "/workspaces/$workspaceId/folders/$folderId", params: { workspaceId, folderId } });
  };

  const handleTaskClick = (taskId: string) => {
    navigate({ to: "/workspaces/$workspaceId/tasks/$taskId", params: { workspaceId, taskId } });
  };

  if (!viewData) return null;

  if (viewMode === "board") {
    return (
      <div className="h-full flex gap-4 p-6 overflow-x-auto no-scrollbar animate-in fade-in slide-in-from-bottom-2 duration-500">
        {groups.map((group) => (
          <StatusGroup
            key={group.statusId}
            statusName={group.statusName}
            color={group.color}
            totalCount={group.tasks.length + group.folders.length}
          >
            {group.folders.map((folder: any) => (
              <FolderItem
                key={folder.id}
                folder={folder}
                onClick={() => handleFolderClick(folder.id)}
              />
            ))}
            {group.tasks.map((task: any) => (
              <TaskItem 
                key={task.id} 
                task={task} 
                onClick={() => handleTaskClick(task.id)} 
              />
            ))}
          </StatusGroup>
        ))}
      </div>
    );
  }

  return (
    <div className="h-full overflow-auto p-8 animate-in fade-in slide-in-from-bottom-2 duration-500">
      <div className="max-w-5xl mx-auto space-y-12">
        {groups.map((group) => (
          <section key={group.statusId} className="space-y-4">
            {/* List Header */}
            <div className="flex items-center gap-3 px-2">
              <div
                className="w-2 h-2 rounded-full"
                style={{ backgroundColor: group.color, boxShadow: `0 0 10px ${group.color}66` }}
              />
              <h3 className="text-[11px] font-black uppercase tracking-[0.2em] text-foreground/70">
                {group.statusName}
              </h3>
              <span className="text-[10px] font-black text-muted-foreground/20 px-2 py-0.5 rounded-full bg-white/[0.02] border border-white/[0.03]">
                {group.tasks.length + group.folders.length}
              </span>
            </div>

            {/* List Table Body */}
            <div className="rounded-2xl border border-white/[0.03] overflow-hidden bg-[#080808]/40 backdrop-blur-sm shadow-2xl">
              {group.folders.map((folder: any, idx: number) => (
                <FolderListRow 
                  key={folder.id} 
                  folder={folder} 
                  isFirst={idx === 0}
                  onClick={() => handleFolderClick(folder.id)}
                />
              ))}
              {group.tasks.map((task: any, idx: number) => (
                <TaskListRow 
                  key={task.id} 
                  task={task} 
                  isFirst={group.folders.length === 0 && idx === 0}
                  onClick={() => handleTaskClick(task.id)}
                />
              ))}
              {group.tasks.length === 0 && group.folders.length === 0 && (
                <div className="h-20 flex items-center justify-center text-[10px] font-black uppercase tracking-widest text-white/[0.02]">
                   Empty Section
                </div>
              )}
            </div>
          </section>
        ))}
      </div>
    </div>
  );
}

function TaskListRow({ task, onClick, isFirst }: { task: any; onClick: () => void; isFirst?: boolean }) {
  const displayId = `T-${task.id.slice(0, 4).toUpperCase()}`;
  
  return (
    <div 
      onClick={onClick}
      className={cn(
        "h-14 px-6 flex items-center gap-6 hover:bg-white/[0.02] transition-all cursor-pointer group border-t border-white/[0.02]",
        isFirst && "border-t-0"
      )}
    >
      <div className="flex items-center gap-4 flex-1 min-w-0">
         <div className="flex items-center gap-1.5 px-2 py-1 rounded-lg bg-white/[0.03] border border-white/[0.05] text-[9px] font-black text-muted-foreground/30 tracking-widest uppercase group-hover:text-muted-foreground/50 transition-colors">
            <Hash className="h-2.5 w-2.5" />
            {displayId}
         </div>
         <span className="text-[13px] font-bold text-foreground/80 group-hover:text-foreground transition-colors truncate">
           {task.name}
         </span>
      </div>

      <div className="flex items-center gap-6 shrink-0">
         {task.dueDate && (
           <div className="flex items-center gap-2 text-muted-foreground/30">
             <Clock className="h-3 w-3" />
             <span className="text-[10px] font-black uppercase tracking-widest">{format(new Date(task.dueDate), "MMM d")}</span>
           </div>
         )}
         
         <div className="h-6 w-6 rounded-full bg-white/[0.03] border border-white/[0.08] flex items-center justify-center group-hover:border-primary/20 transition-all">
           <User className="h-3 w-3 text-muted-foreground/20 group-hover:text-primary/40 transition-colors" />
         </div>
      </div>
    </div>
  );
}

function FolderListRow({ folder, onClick, isFirst }: { folder: any; onClick: () => void; isFirst?: boolean }) {
  const folderColor = folder.color || "#3b82f6";

  return (
    <div 
      onClick={onClick}
      className={cn(
        "h-14 px-6 flex items-center gap-6 hover:bg-white/[0.02] transition-all cursor-pointer group border-t border-white/[0.02] bg-primary/[0.01]",
        isFirst && "border-t-0"
      )}
    >
      <div className="flex items-center gap-4 flex-1 min-w-0">
         <div className="flex items-center gap-2 px-2 py-1 rounded-lg bg-white/[0.03] border border-white/[0.05] text-[9px] font-black text-muted-foreground/30 tracking-widest uppercase group-hover:text-muted-foreground/50 transition-colors">
            <Layers className="h-2.5 w-2.5" />
            Folder
         </div>
         
         <div 
            className="shrink-0 h-6 w-6 rounded-lg flex items-center justify-center border border-white/[0.05] shadow-sm"
            style={{ 
              backgroundColor: `${folderColor}10`,
              color: folderColor 
            }}
         >
            {folder.icon ? <span className="text-[10px] font-black">{folder.icon}</span> : <FolderIcon className="h-3 w-3" />}
         </div>
         
         <span className="text-[13px] font-black text-foreground group-hover:text-primary transition-colors truncate">
           {folder.name}
         </span>
      </div>

      <div className="flex items-center gap-4 shrink-0">
         <div className="flex items-center gap-2">
            <div className="h-1 w-16 bg-white/[0.03] rounded-full overflow-hidden">
               <div className="h-full bg-primary/40 w-1/3 rounded-full" />
            </div>
            <span className="text-[9px] font-black text-muted-foreground/20">35%</span>
         </div>
      </div>
    </div>
  );
}
