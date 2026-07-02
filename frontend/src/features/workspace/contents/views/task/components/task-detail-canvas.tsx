import { useCallback, useLayoutEffect, useMemo, useRef } from "react";
import { observer } from "mobx-react-lite";
import { StatusSelect } from "@/components/status-select";
import { PrioritySelect } from "@/components/priority-select";
import { PriorityBadge } from "@/components/priority-badge";
import { UniversalPicker } from "@/components/universal-picker";
import { DynamicIcon } from "@/components/dynamic-icon";
import { BlockEditor } from "@/components/blockbase/block-editor";
import { TaskViewSkeleton } from "./task-view-skeleton";
import { DateSelect } from "@/components/date-select";
import { DebouncedInput } from "@/components/debounced-input";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { TaskAssignees } from "../task-components/task-assignees";
import { TaskComments } from "../task-components/task-comments";
import { TaskSubtasks } from "../task-components/task-subtasks";
import type { Priority } from "@/types/priority";
import type { TaskRecord } from "@/types/projects/task-record";
import { useStore } from "@/stores/root.store";
import { useSyncEngine, useSyncReady } from "@/sync/sync-provider";
import type { SyncEngine } from "@/sync/sync-engine";
import { useDebouncedFlush } from "@/sync/use-debounced-flush";
import { TaskMutations } from "@/mutations/task.mutations";

interface TaskDetailCanvasProps {
  taskId?: string;
}

// Every field change writes to the store/IndexedDB/transaction queue immediately (all local,
// cheap, and what gives instant optimistic UI) via taskMutations.updateLocal() — only the
// network send is debounced (useDebouncedFlush → syncEngine.flushQueue(), which runs
// TransactionQueue.squash() before sending, merging multiple pending updates for the same task
// into one PUT). N rapid field edits become exactly one network call. (Previously this called
// taskMutations.update(), which fires its own immediate API request every time — debouncing
// when THAT fires just meant debouncing the optimistic UI update too, since update() bundles
// both together.)
function useDebouncedTaskUpdate(taskMutations: TaskMutations, syncEngine: SyncEngine, taskId: string) {
  const taskIdRef = useRef(taskId);
  useLayoutEffect(() => { taskIdRef.current = taskId; });
  const { scheduleFlush } = useDebouncedFlush(syncEngine);

  return useCallback((patches: Partial<TaskRecord>) => {
    taskMutations.updateLocal(taskIdRef.current, patches).catch((err) => console.error("Failed to apply local task update", err));
    scheduleFlush();
  }, [taskMutations, scheduleFlush]);
}

export const TaskDetailCanvas = observer(function TaskDetailCanvas({ taskId }: TaskDetailCanvasProps) {
  const rootStore = useStore();
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

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center h-full text-destructive/80 space-y-2">
        <DynamicIcon name="AlertTriangle" size={32} />
        <span className="text-sm font-medium">Failed to load task</span>
        <span className="text-xs text-muted-foreground">The task may have been deleted, or there was a server error.</span>
      </div>
    );
  }

  if (ready && !task) {
    return (
      <div className="flex flex-col items-center justify-center h-full text-destructive/80 space-y-2">
        <DynamicIcon name="AlertTriangle" size={32} />
        <span className="text-sm font-medium">Task Not Found</span>
        <span className="text-xs text-muted-foreground">The task may have been deleted by another user.</span>
      </div>
    );
  }

  if (!ready || !task) {
    return <TaskViewSkeleton />;
  }

  const handleStatusChange = (statusId: string) => {
    updateTask({ statusId });
  };

  const handlePriorityChange = (priority: Priority) => {
    updateTask({ priority });
  };

  const handleStartDateChange = (date: Date | undefined) => {
    updateTask({ startDate: date ? date.toISOString() : null });
  };

  const handleDueDateChange = (date: Date | undefined) => {
    updateTask({ dueDate: date ? date.toISOString() : null });
  };

  const handleClearDates = () => {
    updateTask({ startDate: null, dueDate: null });
  };

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

          {/* Properties Area */}
          <div className="flex flex-col gap-3.5 pb-6 border-b border-border/30">
            {/* Row 1: Task State and Timing (Status, Priority, Dates) */}
            <div className="flex flex-wrap items-center gap-2.5">
              <StatusSelect
                value={task.statusId ?? undefined}
                onChange={handleStatusChange}
                spaceId={task.spaceId!}
              />

              <PrioritySelect
                value={task.priority}
                onChange={handlePriorityChange}
                trigger={
                  <button type="button" className="cursor-pointer focus:outline-none bg-transparent border-none p-0">
                    <PriorityBadge priority={task.priority} />
                  </button>
                }
              />

              <DateSelect
                startDate={task.startDate}
                dueDate={task.dueDate}
                onStartDateChange={handleStartDateChange}
                onDueDateChange={handleDueDateChange}
                onClearDates={handleClearDates}
                size="sm"
              />
            </div>

            {/* Row 2: People (Assignees) */}
            <TaskAssignees taskId={taskId} />
          </div>

          {/* Document Section */}
          {task.defaultDocumentId && (
            <BlockEditor key={task.defaultDocumentId} documentId={task.defaultDocumentId} editable={canEdit} />
          )}

          {/* Subtasks Section (Only for top-level tasks) */}
          {!task.parentTaskId && <TaskSubtasks taskId={taskId} />}

          {/* Comments Section */}
          <TaskComments taskId={taskId} />
        </div>
      </div>
    </div>
  );
});
