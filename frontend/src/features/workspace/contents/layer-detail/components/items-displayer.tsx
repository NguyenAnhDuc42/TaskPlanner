import { useMemo } from "react";

import { StatusGroup } from "./status-group";
import { TaskItem } from "./task-item";
import { FolderItem } from "./folder-item";
import type { ItemsViewMode, TaskViewData } from "../layer-detail-types";

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

  if (!viewData) return null;

  if (viewMode === "board") {
    return (
      <div className="h-full flex gap-3 p-4 overflow-x-auto no-scrollbar animate-in fade-in duration-300">
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
                onClick={() => {}}
              />
            ))}
            {group.tasks.map((task: any) => (
              <TaskItem key={task.id} task={task} onClick={() => {}} />
            ))}
          </StatusGroup>
        ))}
      </div>
    );
  }

  return (
    <div className="h-full overflow-auto p-4 animate-in fade-in duration-300">
      <div className="max-w-5xl mx-auto space-y-8">
        {groups.map((group) => (
          <section key={group.statusId} className="space-y-2">
            <div className="flex items-center gap-2 px-2">
              <div
                className="w-1.5 h-1.5 rounded-full"
                style={{ backgroundColor: group.color }}
              />
              <h3 className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/60">
                {group.statusName}
              </h3>
              <span className="text-[9px] font-bold text-muted-foreground/20">
                {group.tasks.length + group.folders.length}
              </span>
            </div>
            <div className="space-y-px border border-border/40 rounded-lg overflow-hidden bg-muted/5 shadow-sm">
              {group.folders.map((folder: any) => (
                <FolderListRow key={folder.id} folder={folder} />
              ))}
              {group.tasks.map((task: any) => (
                <TaskListRow key={task.id} task={task} />
              ))}
            </div>
          </section>
        ))}
      </div>
    </div>
  );
}

function TaskListRow({ task }: { task: any }) {
  return (
    <div className="h-10 px-4 flex items-center gap-4 hover:bg-muted/40 transition-colors cursor-pointer group border-b border-border/5 last:border-none">
      <div className="h-4 w-4 flex-shrink-0 flex items-center justify-center">
        <div className="h-2 w-2 rounded-full border border-muted-foreground/30" />
      </div>
      <span className="text-[12px] font-medium text-foreground/80 truncate flex-1">
        {task.name}
      </span>
    </div>
  );
}

function FolderListRow({ folder }: { folder: any }) {
  return (
    <div className="h-10 px-4 flex items-center gap-4 hover:bg-muted/40 transition-colors cursor-pointer group border-b border-border/5 last:border-none bg-primary/[0.02]">
      <div className="h-4 w-4 flex-shrink-0 flex items-center justify-center text-primary/60">
        <span className="text-[10px] font-black">{folder.icon || "F"}</span>
      </div>
      <span className="text-[12px] font-bold text-foreground truncate flex-1">
        {folder.name}
      </span>
    </div>
  );
}
