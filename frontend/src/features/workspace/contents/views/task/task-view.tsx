import { useEffect, useMemo, useState } from "react";
import { observer } from "mobx-react-lite";
import { TaskDetailCanvas } from "./components/task-detail-canvas";
import { useDebouncedTaskUpdate } from "./components/use-debounced-task-update";
import { TaskPropertiesPanel } from "./components/task-properties-panel";
import { TaskListPanel } from "./components/task-list-panel";
import { Sheet, SheetContent, SheetTitle } from "@/components/ui/sheet";
import { useIsMobile } from "@/hooks/use-mobile";
import { PanelRight, X } from "lucide-react";
import type { Priority } from "@/types/priority";
import { useEntityShellUi } from "../entity-shell";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { TaskMutations } from "@/mutations/task.mutations";
import { useLocalStorage } from "@/hooks/use-local-storage";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { cn } from "@/lib/utils";

interface TaskViewBodyProps {
  taskId: string;
}

export const TaskViewBody = observer(function TaskViewBody({ taskId }: Readonly<TaskViewBodyProps>) {
  const isMobile = useIsMobile();
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const taskMutations = useMemo(() => new TaskMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const [isPropertiesOpen, setIsPropertiesOpen] = useState(false);
  const { workspaceId } = useWorkspace();
  // Persisted (not plain useState) — the route remounts this whole component on every task
  // switch (`key={taskId}` in $taskId.tsx), and the point of this list is to browse between
  // tasks, so it shouldn't snap shut on every click the way the per-task properties panel does.
  const [isTaskListOpen, setIsTaskListOpen] = useLocalStorage(`task-list-panel-open:${workspaceId}`, false);
  const task = rootStore.taskStore.getById(taskId);
  const updateTask = useDebouncedTaskUpdate(taskMutations, syncEngine, taskId);

  const shellUi = useEntityShellUi();
  useEffect(() => {
    shellUi?.setRightPanelOpen(isPropertiesOpen && !isMobile);
    return () => shellUi?.setRightPanelOpen(false);
  }, [isPropertiesOpen, isMobile, shellUi]);

  return (
    <div className="h-full w-full flex bg-card overflow-hidden relative">
      {task?.spaceId && !isMobile && (
        <div
          className={cn(
            "shrink-0 overflow-hidden transition-[width] duration-200 ease-in-out",
            isTaskListOpen ? "w-52 border-r border-border/15" : "w-0",
          )}
        >
          <div className="w-52 h-full flex flex-col">
            <div className="h-9 flex items-center justify-between px-2 border-b border-border/30 shrink-0">
              <span className="text-[9px] font-bold uppercase tracking-wider text-muted-foreground/40 px-1">Tasks</span>
              <button
                type="button"
                onClick={() => setIsTaskListOpen(false)}
                title="Close task list"
                className="h-6 w-6 flex items-center justify-center rounded-md text-muted-foreground hover:text-foreground hover:bg-muted/60 transition-colors cursor-pointer"
              >
                <X className="h-3.5 w-3.5" />
              </button>
            </div>
            <TaskListPanel spaceId={task.spaceId} activeTaskId={taskId} />
          </div>
        </div>
      )}

      <div className="flex-1 flex flex-col overflow-hidden relative">
        <TaskDetailCanvas taskId={taskId} />

        {!isTaskListOpen && task?.spaceId && !isMobile && (
          <button
            type="button"
            onClick={() => setIsTaskListOpen(true)}
            title="Open task list"
            className="absolute top-4 left-0 z-10 h-16 w-5 flex items-center justify-center rounded-r-md border-y border-r border-border/30 transition-colors cursor-pointer bg-muted/30 text-muted-foreground hover:text-foreground hover:bg-muted/60"
          >
            <span className="rotate-90 whitespace-nowrap text-[9px] font-bold uppercase tracking-wider">
              Tasks
            </span>
          </button>
        )}

        {!isPropertiesOpen && (
          <button
            type="button"
            onClick={() => setIsPropertiesOpen(true)}
            title="Open properties panel"
            className="absolute top-3 right-4 z-10 h-7 w-7 flex items-center justify-center rounded-md border border-border/30 shadow-sm transition-colors cursor-pointer bg-card/80 backdrop-blur-sm text-muted-foreground hover:text-foreground hover:bg-muted/60"
          >
            <PanelRight className="h-3.5 w-3.5" />
          </button>
        )}
      </div>

      {isPropertiesOpen && task && !isMobile && (
        <div className="w-64 shrink-0 overflow-hidden flex flex-col">
          <div className="h-9 flex items-center justify-end px-2 border-b border-border/30 shrink-0">
            <button
              type="button"
              onClick={() => setIsPropertiesOpen(false)}
              title="Close properties panel"
              className="h-6 w-6 flex items-center justify-center rounded-md text-muted-foreground hover:text-foreground hover:bg-muted/60 transition-colors cursor-pointer"
            >
              <X className="h-3.5 w-3.5" />
            </button>
          </div>
          <TaskPropertiesPanel
            task={task}
            onStatusChange={(statusId) => updateTask({ statusId })}
            onPriorityChange={(priority: Priority) => updateTask({ priority })}
            onStartDateChange={(date) => updateTask({ startDate: date ? date.toISOString() : null })}
            onDueDateChange={(date) => updateTask({ dueDate: date ? date.toISOString() : null })}
            onClearDates={() => updateTask({ startDate: null, dueDate: null })}
          />
        </div>
      )}

      {isMobile && task && (
        <Sheet open={isPropertiesOpen} onOpenChange={setIsPropertiesOpen}>
          <SheetContent side="right" className="w-full sm:max-w-sm p-0 flex flex-col">
            <SheetTitle className="sr-only">Task Properties</SheetTitle>
            <div className="h-9 shrink-0" />
            <TaskPropertiesPanel
              task={task}
              onStatusChange={(statusId) => updateTask({ statusId })}
              onPriorityChange={(priority: Priority) => updateTask({ priority })}
              onStartDateChange={(date) => updateTask({ startDate: date ? date.toISOString() : null })}
              onDueDateChange={(date) => updateTask({ dueDate: date ? date.toISOString() : null })}
              onClearDates={() => updateTask({ startDate: null, dueDate: null })}
            />
          </SheetContent>
        </Sheet>
      )}
    </div>
  );
});
