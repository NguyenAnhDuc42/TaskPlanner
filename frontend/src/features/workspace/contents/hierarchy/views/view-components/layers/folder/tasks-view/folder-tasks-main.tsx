import type { TaskItemDto, TaskViewData } from "../../../../views-type";
import { StatusGroup } from "../../../item-components/status-group";
import { TaskItem } from "../../../item-components/task-item";

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
  if (!data?.groups) return null;

  return (
    <div className="flex-1 flex flex-col p-12 overflow-y-auto no-scrollbar">
      <div className="space-y-12 max-w-5xl w-full mx-auto">
        {data.groups.map((group) => (
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
        ))}
      </div>
    </div>
  );
}
