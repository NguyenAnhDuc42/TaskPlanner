import type { FolderItemDto, TaskItemDto, TaskViewData } from "../../../../views-type";
import { StatusGroup } from "../../../item-components/status-group";
import { FolderItem } from "../../../item-components/folder-item";
import { TaskItem } from "../../../item-components/task-item";
import { useMemo } from "react";

interface SpaceTasksMainProps {
  data: TaskViewData;
  onFolderSelect: (folder: FolderItemDto) => void;
  onTaskSelect: (task: TaskItemDto) => void;
  selectedId?: string;
}

export function SpaceTasksMain({
  data,
  onFolderSelect,
  onTaskSelect,
  selectedId,
}: SpaceTasksMainProps) {
  
  const groups = useMemo(() => {
    if (!data) return [];

    const { folders, tasks, statuses } = data;

    const statusGroups = statuses.map(status => ({
      statusId: status.statusId,
      statusName: status.name,
      color: status.color,
      category: status.category,
      folders: folders.filter(f => f.statusId === status.statusId),
      tasks: tasks.filter(t => t.statusId === status.statusId)
    }));

    const orphanedFolders = folders.filter(f => !f.statusId || !statuses.find(s => s.statusId === f.statusId));
    const orphanedTasks = tasks.filter(t => !t.statusId || !statuses.find(s => s.statusId === t.statusId));

    if (orphanedFolders.length > 0 || orphanedTasks.length > 0) {
      return [
        {
          statusId: "no-status",
          statusName: "No Status",
          color: "#94a3b8",
          category: "NotStarted",
          folders: orphanedFolders,
          tasks: orphanedTasks
        },
        ...statusGroups
      ];
    }

    return statusGroups;
  }, [data]);

  if (!data) return null;

  return (
    <div className="flex-1 overflow-hidden">
      {/* Horizontal Board Container with reduced gap */}
      <div className="h-full flex gap-3 p-6 overflow-x-auto no-scrollbar">
        {groups.map((group) => {
          const totalCount = group.folders.length + group.tasks.length;
          
          return (
            <StatusGroup
              key={group.statusId}
              statusName={group.statusName}
              color={group.color}
              totalCount={totalCount}
            >
              {/* FOLDERS FIRST */}
              {group.folders.map((folder) => (
                <FolderItem
                  key={folder.id}
                  folder={folder}
                  onClick={onFolderSelect}
                  isSelected={selectedId === folder.id}
                />
              ))}

              {/* TASKS */}
              {group.tasks.map((task) => (
                <TaskItem
                  key={task.id}
                  task={task}
                  onClick={onTaskSelect}
                  isSelected={selectedId === task.id}
                />
              ))}
            </StatusGroup>
          );
        })}

        {/* Padding for the end of horizontal scroll */}
        <div className="w-6 flex-shrink-0" />
      </div>
    </div>
  );
}
