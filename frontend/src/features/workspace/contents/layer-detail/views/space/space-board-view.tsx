import { useMemo } from "react";
import { useNavigate, useParams } from "@tanstack/react-router";
import { StatusGroup } from "../../components/items/status-group";
import { TaskItem } from "../../components/items/task-item";
import { FolderItem } from "../../components/items/folder-item";
import type { TaskViewData } from "../../layer-detail-types";

interface SpaceBoardViewProps {
  viewData: TaskViewData;
}

export function SpaceBoardView({ viewData }: SpaceBoardViewProps) {
  const navigate = useNavigate();
  const { workspaceId } = useParams({ strict: false }) as any;

  const groups = useMemo(() => {
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
