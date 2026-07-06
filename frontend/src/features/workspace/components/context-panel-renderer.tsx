import { useMemo } from "react";
import { observer } from "mobx-react-lite";
import { TaskDetailCanvas } from "@/features/workspace/contents/views/task/components/task-detail-canvas";
import { FolderTaskList } from "@/features/workspace/contents/views/folder/components/folder-task-list";
import { useNavigate, useLocation } from "@tanstack/react-router";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { useDebouncedFlush } from "@/sync/use-debounced-flush";
import { TaskMutations } from "@/mutations/task.mutations";

interface ContextPanelRendererProps {
  data: {
    type: "task" | "folder" | "space" | "project";
    id: string;
  };
}

export const ContextPanelRenderer = observer(function ContextPanelRenderer({ data }: ContextPanelRendererProps) {
  const navigate = useNavigate({ from: "/workspaces/$workspaceId" });
  const location = useLocation();
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const taskMutations = useMemo(() => new TaskMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const { scheduleFlush } = useDebouncedFlush(syncEngine);

  // TODO: Wire up real detail components as they're built
  if (data.type === "task" && data.id) {
    return <TaskDetailCanvas taskId={data.id} />;
  }
  if (data.type === "folder" && data.id) {
    const folder = rootStore.folderStore.getById(data.id);
    const tasks = rootStore.taskStore.getByFolder(data.id).filter((t) => !t.parentTaskId);
    const taskStatuses = folder?.spaceId ? rootStore.statusStore.getBySpace(folder.spaceId) : [];

    return (
      <div className="flex flex-col h-full bg-card rounded-md shadow-sm border border-border/40 overflow-hidden">
        <FolderTaskList
          folderId={data.id}
          tasks={tasks}
          taskStatuses={taskStatuses}
          spaceId={folder?.spaceId}
          taskMutations={taskMutations}
          scheduleFlush={scheduleFlush}
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
});
