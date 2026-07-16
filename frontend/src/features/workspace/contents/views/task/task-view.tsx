import { useEffect, useMemo, useState } from "react";
import { observer } from "mobx-react-lite";
import { TaskDetailCanvas } from "./components/task-detail-canvas";
import { useDebouncedTaskUpdate } from "./components/use-debounced-task-update";
import { TaskPropertiesPanel } from "./components/task-properties-panel";
import { Sheet, SheetContent, SheetTitle } from "@/components/ui/sheet";
import { useIsMobile } from "@/hooks/use-mobile";
import { PanelRight, X } from "lucide-react";
import type { Priority } from "@/types/priority";
import { useEntityShellUi } from "../entity-shell";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { TaskMutations } from "@/mutations/task.mutations";

interface TaskViewBodyProps {
  taskId: string;
}

export const TaskViewBody = observer(function TaskViewBody({ taskId }: Readonly<TaskViewBodyProps>) {
  const isMobile = useIsMobile();
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const taskMutations = useMemo(() => new TaskMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const [isPropertiesOpen, setIsPropertiesOpen] = useState(false);
  const task = rootStore.taskStore.getById(taskId);
  const updateTask = useDebouncedTaskUpdate(taskMutations, syncEngine, taskId);

  const shellUi = useEntityShellUi();
  useEffect(() => {
    shellUi?.setRightPanelOpen(isPropertiesOpen && !isMobile);
    return () => shellUi?.setRightPanelOpen(false);
  }, [isPropertiesOpen, isMobile, shellUi]);

  return (
    <div className="h-full w-full flex bg-card overflow-hidden relative">
      <div className="flex-1 flex flex-col overflow-hidden relative">
        <TaskDetailCanvas taskId={taskId} />

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
