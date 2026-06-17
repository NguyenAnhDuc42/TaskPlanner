import { TaskDetailCanvas } from "@/features/workspace/contents/views/task/components/task-detail-canvas";
import { FolderTaskList } from "@/features/workspace/contents/views/folder/components/folder-task-list";
import { useNavigate, useLocation } from "@tanstack/react-router";

interface ContextPanelRendererProps {
  data: { 
    type: "task" | "folder" | "space" | "project";
    id: string; 
  };
}

export function ContextPanelRenderer({ data }: ContextPanelRendererProps) {
  const navigate = useNavigate({ from: "/workspaces/$workspaceId" });
  const location = useLocation();

  // TODO: Wire up real detail components as they're built
  if (data.type === "task" && data.id) {
    return <TaskDetailCanvas taskId={data.id} />;
  }
  if (data.type === "folder" && data.id) {
    return (
      <div className="flex flex-col h-full bg-card rounded-md shadow-sm border border-border/40 overflow-hidden">
        <FolderTaskList 
          folderIdProp={data.id} 
          onSelectTask={(taskId) => {
            navigate({
              to: location.pathname,
              search: (prev: Record<string, unknown>) => {
                const searchParams = prev;
                return {
                  ...searchParams,
                  contextPanel: { type: "task", id: taskId }
                };
              }
            });
          }} 
        />
      </div>
    );
  }
  // if (data.type === "space") return <SpaceDetail space={data} />;

  return (
    <pre className="text-xs text-muted-foreground p-2">
      {JSON.stringify(data, null, 2)}
    </pre>
  );
}
