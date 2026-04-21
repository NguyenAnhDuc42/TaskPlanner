import type { FolderItemDto, TaskItemDto, TaskViewData } from "../../../../views-type";
import { StatusGroup } from "../../../item-components/status-group";
import { FolderItem } from "../../../item-components/folder-item";
import { TaskItem } from "../../../item-components/task-item";

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
  if (!data?.groups) return null;

  return (
    <div className="flex-1 flex flex-col p-12 overflow-y-auto no-scrollbar">
      <div className="space-y-12 max-w-5xl w-full mx-auto">
        {data.groups.map((group) => {
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
      </div>
    </div>
  );
}
