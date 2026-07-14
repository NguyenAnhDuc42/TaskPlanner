import { Suspense, useMemo } from "react";
import { observer } from "mobx-react-lite";
import { UniversalPicker } from "@/components/universal-picker";
import { DynamicIcon } from "@/components/dynamic-icon";
import { LoadingScreen } from "@/components/loading-screen";
import { TaskViewSkeleton } from "./task-view-skeleton";
import { useDebouncedTaskUpdate } from "./use-debounced-task-update";
import { DebouncedInput } from "@/components/debounced-input";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { TaskComments } from "../task-components/task-comments";
import { TaskSubtasks } from "../task-components/task-subtasks";
import { ExpandableSection } from "@/components/expandable-section";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine, useSyncReady } from "@/sync/sync-provider";
import { TaskMutations } from "@/mutations/task.mutations";
import { LazyBlockEditor as BlockEditor } from "@/components/blockbase/lazy-block-editor";

interface TaskDetailCanvasProps {
  taskId?: string;
}

export const TaskDetailCanvas = observer(function TaskDetailCanvas({ taskId }: TaskDetailCanvasProps) {
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const { ready, error } = useSyncReady();
  const taskMutations = useMemo(() => new TaskMutations(rootStore, syncEngine), [rootStore, syncEngine]);

  const task = taskId ? rootStore.taskStore.getById(taskId) : undefined;
  const updateTask = useDebouncedTaskUpdate(taskMutations, syncEngine, taskId || "");
  const { canCreateContent: canEdit } = useWorkspaceRole();

  if (!taskId) {
    return (
      <div className="flex items-center justify-center h-full text-muted-foreground text-sm italic">
        No task selected.
      </div>
    );
  }

  if (!task) {
    if (error) {
      return (
        <div className="flex flex-col items-center justify-center h-full text-destructive/80 space-y-2">
          <DynamicIcon name="AlertTriangle" size={32} />
          <span className="text-sm font-medium">Failed to load task</span>
          <span className="text-xs text-muted-foreground">The task may have been deleted, or there was a server error.</span>
        </div>
      );
    }

    if (ready) {
      return (
        <div className="flex flex-col items-center justify-center h-full text-destructive/80 space-y-2">
          <DynamicIcon name="AlertTriangle" size={32} />
          <span className="text-sm font-medium">Task Not Found</span>
          <span className="text-xs text-muted-foreground">The task may have been deleted by another user.</span>
        </div>
      );
    }

    return <TaskViewSkeleton />;
  }

  return (
    <div className="flex flex-col h-full w-full bg-card overflow-hidden">
      {/* Task Content Scroll Area */}
      <div className="flex-1 overflow-y-auto [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/45 [&::-webkit-scrollbar-track]:bg-transparent">
        <div className="w-full p-4 md:p-8 space-y-6">
          {/* Header Title Area */}
          <div className="flex items-start gap-3">
            <UniversalPicker
              icon={task.icon || "CheckSquare"}
              color={task.color || "#6366f1"}
              onSelect={(icon, color) => updateTask({ icon, color })}
              size="lg"
            />

            <DebouncedInput
              value={task.name || ""}
              onChange={(val) => {
                if (val.trim() && val !== task.name) {
                  updateTask({ name: val.trim() });
                }
              }}
              debounceMs={1500}
              placeholder="Untitled Task"
              className="text-2xl font-black text-foreground border-none p-0 focus-visible:ring-0 bg-transparent h-auto outline-none w-full"
            />
          </div>

          {/* Document Section */}
          {task.defaultDocumentId && (
            <Suspense fallback={<LoadingScreen className="min-h-0 py-6" />}>
              <BlockEditor key={task.defaultDocumentId} documentId={task.defaultDocumentId} editable={canEdit} />
            </Suspense>
          )}

          {/* Subtasks Section (Only for top-level tasks) */}
          {!task.parentTaskId && (
            <ExpandableSection title="Subtasks">
              <TaskSubtasks taskId={taskId} />
            </ExpandableSection>
          )}

          {/* Comments Section */}
          <ExpandableSection title="Comments">
            <TaskComments taskId={taskId} />
          </ExpandableSection>
        </div>
      </div>
    </div>
  );
});
