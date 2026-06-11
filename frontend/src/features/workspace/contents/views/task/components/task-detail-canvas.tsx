import { useState } from "react";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { useSelector } from "react-redux";
import { taskSelectors } from "@/store/entityStore";
import type { RootState } from "@/store";
import { StatusSelect } from "@/components/status-select";
import { PrioritySelect } from "@/components/priority-select";
import { PriorityBadge } from "@/components/priority-badge";
import { UniversalPicker } from "@/components/universal-picker";
import { DynamicIcon } from "@/components/dynamic-icon";
import { BlockEditor } from "@/components/blockbase/block-editor";
import { ViewSkeleton } from "@/components/view-skeleton";
import { DateSelect } from "@/components/date-select";
import { useGetTaskDetailQuery, useUpdateTaskMutation } from "../task-api";
import { TaskAssignees } from "../task-components/task-assignees";
import { TaskComments } from "../task-components/task-comments";
import { TaskSubtasks } from "../task-components/task-subtasks";
import type { Priority } from "@/types/priority";

interface TaskDetailCanvasProps {
  taskId?: string;
}

export function TaskDetailCanvas({ taskId }: TaskDetailCanvasProps) {
  const { isLoading } = useGetTaskDetailQuery(taskId || "", {
    skip: !taskId,
  });
  const task = useSelector((state: RootState) => taskSelectors.selectById(state, taskId || ""));
  const [updateTask] = useUpdateTaskMutation();

  const [localName, setLocalName] = useState("");
  const [prevTaskName, setPrevTaskName] = useState("");

  if (task?.name && task.name !== prevTaskName) {
    setPrevTaskName(task.name);
    setLocalName(task.name);
  }

  if (!taskId) {
    return (
      <div className="flex items-center justify-center h-full text-muted-foreground text-sm italic">
        No task selected.
      </div>
    );
  }

  if (isLoading || !task) {
    return <ViewSkeleton />;
  }

  const handleNameBlur = () => {
    if (localName.trim() && localName !== task.name) {
      updateTask({ taskId, patches: { name: localName.trim() } });
    }
  };

  const handleStatusChange = (statusId: string) => {
    updateTask({ taskId, patches: { statusId } });
  };

  const handlePriorityChange = (priority: Priority) => {
    updateTask({ taskId, patches: { priority } });
  };

  const handleStartDateChange = (date: Date | undefined) => {
    updateTask({ taskId, patches: { startDate: date ? date.toISOString() : undefined } });
  };

  const handleDueDateChange = (date: Date | undefined) => {
    updateTask({ taskId, patches: { dueDate: date ? date.toISOString() : undefined } });
  };

  return (
    <div className="flex flex-col h-full w-full bg-transparent overflow-hidden">
      {/* Task Content Scroll Area */}
      <div className="flex-1 overflow-y-auto [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/45 [&::-webkit-scrollbar-track]:bg-transparent">
        <div className="w-full p-4 md:p-8 space-y-6">
          {/* Header Title Area */}
          <div className="flex items-start gap-3">
            <Popover>
              <PopoverTrigger asChild>
                <button className="h-9 w-9 flex items-center justify-center rounded-lg border border-border/50 bg-muted/20 hover:bg-muted/40 transition-colors shrink-0">
                  <DynamicIcon name={task.icon || "CheckSquare"} color={task.color || ""} size={24} />
                </button>
              </PopoverTrigger>
              <PopoverContent className="p-0 border-none bg-transparent shadow-none" align="start">
                <UniversalPicker
                  selectedIcon={task.icon || "CheckSquare"}
                  selectedColor={task.color || "#6366f1"}
                  onSelect={(icon, color) => {
                    updateTask({ taskId, patches: { icon, color } });
                  }}
                />
              </PopoverContent>
            </Popover>

            <input
              value={localName}
              onChange={(e) => setLocalName(e.target.value)}
              onBlur={handleNameBlur}
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
                workflowId={task.parentWorkflowId}
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
                size="sm"
              />
            </div>

            {/* Row 2: People (Assignees) */}
            <TaskAssignees taskId={taskId} spaceId={task.spaceId} />
          </div>

          {/* Document Section (Rich Text Editor) */}
          <div className="space-y-3">
            <h3 className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground/70 border-b border-border/50 pb-2">
              Document
            </h3>
            {task.defaultDocumentId ? (
              <div className="min-h-[150px] border border-border/10 rounded-lg p-2 bg-muted/5">
                <BlockEditor documentId={task.defaultDocumentId} placeholder="Type '/' for commands..." />
              </div>
            ) : (
              <div className="text-xs text-muted-foreground italic">No document available for this task.</div>
            )}
          </div>

          {/* Subtasks Section */}
          <TaskSubtasks taskId={taskId} />

          {/* Comments Section */}
          <TaskComments taskId={taskId} />
        </div>
      </div>
    </div>
  );
}
