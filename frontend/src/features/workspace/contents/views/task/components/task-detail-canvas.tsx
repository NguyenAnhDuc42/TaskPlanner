import { useSelector } from "react-redux";
import { taskSelectors } from "@/store/entityStore";
import type { RootState } from "@/store";
import { StatusSelect } from "@/components/status-select";
import { PrioritySelect } from "@/components/priority-select";
import { PriorityBadge } from "@/components/priority-badge";
import { UniversalPicker } from "@/components/universal-picker";
import { DynamicIcon } from "@/components/dynamic-icon";
import { BlockEditor } from "@/components/blockbase/block-editor";
import { TaskViewSkeleton } from "./task-view-skeleton";
import { DateSelect } from "@/components/date-select";
import { DebouncedInput } from "@/components/debounced-input";
import { useGetTaskDetailQuery, useDebouncedTaskUpdate } from "../task-api";
import { useSpaceAccess } from "@/features/workspace/context/use-space-access";
import { TaskAssignees } from "../task-components/task-assignees";
import { TaskComments } from "../task-components/task-comments";
import { TaskSubtasks } from "../task-components/task-subtasks";
import type { Priority } from "@/types/priority";

interface TaskDetailCanvasProps {
  taskId?: string;
}

export function TaskDetailCanvas({ taskId }: TaskDetailCanvasProps) {
  const { isLoading, isError } = useGetTaskDetailQuery(taskId || "", {
    skip: !taskId,
  });
  const task = useSelector((state: RootState) => taskSelectors.selectById(state, taskId || ""));
  const updateTask = useDebouncedTaskUpdate(taskId || "");
  const { canEdit } = useSpaceAccess(task?.spaceId ?? "");

  if (!taskId) {
    return (
      <div className="flex items-center justify-center h-full text-muted-foreground text-sm italic">
        No task selected.
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex flex-col items-center justify-center h-full text-destructive/80 space-y-2">
        <DynamicIcon name="AlertTriangle" size={32} />
        <span className="text-sm font-medium">Failed to load task</span>
        <span className="text-xs text-muted-foreground">The task may have been deleted, or there was a server error.</span>
      </div>
    );
  }

  if (!isLoading && !task) {
    return (
      <div className="flex flex-col items-center justify-center h-full text-destructive/80 space-y-2">
        <DynamicIcon name="AlertTriangle" size={32} />
        <span className="text-sm font-medium">Task Not Found</span>
        <span className="text-xs text-muted-foreground">The task may have been deleted by another user.</span>
      </div>
    );
  }

  if (isLoading) {
    return <TaskViewSkeleton />;
  }



  const handleStatusChange = (statusId: string) => {
    updateTask({ statusId });
  };

  const handlePriorityChange = (priority: Priority) => {
    updateTask({ priority });
  };

  const handleStartDateChange = (date: Date | undefined) => {
    updateTask(date ? { startDate: date.toISOString() } : { clearStartDate: true });
  };

  const handleDueDateChange = (date: Date | undefined) => {
    updateTask(date ? { dueDate: date.toISOString() } : { clearDueDate: true });
  };

  const handleClearDates = () => {
    updateTask({ clearStartDate: true, clearDueDate: true });
  };

  return (
    <div className="flex flex-col h-full w-full bg-transparent overflow-hidden">
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
                value={task.statusId}
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
            <TaskAssignees taskId={taskId} spaceId={task.spaceId} />
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
}
