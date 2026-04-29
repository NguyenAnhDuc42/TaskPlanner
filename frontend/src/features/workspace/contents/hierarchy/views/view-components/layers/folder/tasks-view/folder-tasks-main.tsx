import type { TaskItemDto, TaskViewData } from "../../../../views-type";
import { StatusGroup } from "../../../item-components/status-group";
import { TaskItem } from "../../../item-components/task-item";
import { useMemo } from "react";

interface FolderTasksMainProps {
  data: TaskViewData;
  onTaskSelect: (task: TaskItemDto) => void;
  selectedId?: string;
}

export function FolderTasksMain({
  data,
  onTaskSelect,
  selectedId,
}: FolderTasksMainProps) {
  
  const groups = useMemo(() => {
    if (!data) return [];
    const { tasks, statuses } = data;

    const statusGroups = statuses.map(status => ({
      statusId: status.statusId,
      statusName: status.name,
      color: status.color,
      tasks: tasks.filter(t => t.statusId === status.statusId)
    }));

    const orphanedTasks = tasks.filter(t => !t.statusId || !statuses.find(s => s.statusId === t.statusId));

    if (orphanedTasks.length > 0) {
      return [
        {
          statusId: "no-status",
          statusName: "No Status",
          color: "#94a3b8",
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
      <div className="h-full flex gap-3 p-6 overflow-x-auto no-scrollbar">
        {groups.map((group) => {
          return (
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
                  isSelected={selectedId === task.id}
                />
              ))}
            </StatusGroup>
          );
        })}
        <div className="w-6 flex-shrink-0" />
      </div>
    </div>
  );
}
