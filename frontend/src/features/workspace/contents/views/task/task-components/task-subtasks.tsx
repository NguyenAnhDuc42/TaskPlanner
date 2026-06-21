import { useRef, useState, useMemo } from "react";
import { useSelector } from "react-redux";
import { createSelector } from "@reduxjs/toolkit";
import { Trash2, Maximize2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { taskSelectors } from "@/store/entityStore";
import type { RootState } from "@/store";
import {
  useCreateSubTaskMutation,
  useUpdateTaskMutation,
  type UpdateTaskPayload,
} from "../task-api";
import { StatusSelect } from "@/components/status-select";
import { PrioritySelect } from "@/components/priority-select";
import { PriorityBadge } from "@/components/priority-badge";
import { DateSelect } from "@/components/date-select";
import { DebouncedInput } from "@/components/debounced-input";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { useDeleteTaskMutation } from "../../../hierarchy/hierarchy-api";
import type { Priority } from "@/types/priority";
import { useNavigate, useLocation } from "@tanstack/react-router";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";

interface TaskSubtasksProps {
  taskId: string;
}

export function TaskSubtasks({ taskId }: Readonly<TaskSubtasksProps>) {
  const { workspaceId } = useWorkspace();
  const { canCreateContent } = useWorkspaceRole();
  const navigate = useNavigate();

  const selectSubtasks = useMemo(() => 
    createSelector(
      [taskSelectors.selectAll],
      (tasks) => tasks.filter((t) => t.parentTaskId === taskId)
    ),
  [taskId]);

  const subtasks = useSelector(selectSubtasks);

  const parentTask = useSelector((state: RootState) =>
    taskSelectors.selectById(state, taskId)
  );

  const [createSubTask, { isLoading: isCreating }] = useCreateSubTaskMutation();
  const [updateTaskMutation] = useUpdateTaskMutation();
  const [deleteTaskMutation] = useDeleteTaskMutation();

  const updateSubtask = ({ subtaskId, patches }: { subtaskId: string; patches: UpdateTaskPayload }) => {
    updateTaskMutation({ taskId: subtaskId, patches });
  };

  const deleteSubtask = ({ subtaskId }: { subtaskId: string }) => {
    deleteTaskMutation({ workspaceId: workspaceId || "", taskId: subtaskId });
  };

  // New subtask draft state
  const [draftName, setDraftName] = useState("");
  const [draftStatusId, setDraftStatusId] = useState<string | undefined>(undefined);
  const [draftPriority, setDraftPriority] = useState<Priority>("Low");
  const [deleteSubtaskId, setDeleteSubtaskId] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleCreateSubtask = async () => {
    if (!draftName.trim() || isCreating) return;
    try {
      await createSubTask({
        parentTaskId: taskId,
        name: draftName.trim(),
        priority: draftPriority,
        statusId: draftStatusId,
      }).unwrap();
      setDraftName("");
      setDraftStatusId(undefined);
      setDraftPriority("Low");
    } catch (err) {
      console.error("Failed to create subtask", err);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      e.preventDefault();
      handleCreateSubtask();
    }
  };

  const handleStatusChange = (subtaskId: string, statusId: string) => {
    updateSubtask({ subtaskId, patches: { statusId } });
  };

  const handlePriorityChange = (subtaskId: string, priority: Priority) => {
    updateSubtask({ subtaskId, patches: { priority } });
  };

  const handleDateChange = (subtaskId: string, field: "startDate" | "dueDate", date: Date | undefined) => {
    updateSubtask({
      subtaskId,
      patches: date
        ? { [field]: date.toISOString() }
        : { [field === "startDate" ? "clearStartDate" : "clearDueDate"]: true },
    });
  };

  const handleDeleteSubtask = (subtaskId: string) => {
    setDeleteSubtaskId(subtaskId);
  };

  const confirmDelete = () => {
    if (deleteSubtaskId) {
      deleteSubtask({ subtaskId: deleteSubtaskId });
      setDeleteSubtaskId(null);
    }
  };

  const location = useLocation();
  const handleOpenSubtask = (subtaskId: string) => {
    navigate({
      to: location.pathname,
      search: {
        ...(location.search as Record<string, unknown>),
        contextPanel: { type: "task", id: subtaskId },
      },
    });
  };

  return (
    <div className="space-y-3 pt-5 border-t border-border/30">
      <h3 className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground/70">
        Subtasks
      </h3>

      <div className="space-y-1">
        {/* Existing subtasks */}
        {subtasks.map((subtask) => (
          <div
            key={subtask.id}
            onDoubleClick={() => handleOpenSubtask(subtask.id)}
            className="group flex items-center justify-between gap-2 px-2 py-1 rounded-md border border-border/40 hover:border-border/70 bg-muted/10 hover:bg-muted/20 transition-all select-none"
          >
            {/* Left: Status + Name */}
            <div className="flex items-center gap-2 flex-1 min-w-0">
              <div className="shrink-0">
                <StatusSelect
                  value={subtask.statusId || ""}
                  onChange={(statusId) => handleStatusChange(subtask.id, statusId)}
                  workflowId={parentTask?.parentWorkflowId}
                />
              </div>
              <DebouncedInput
                value={subtask.name}
                onChange={(val) => {
                  if (val.trim() && val !== subtask.name) {
                    updateSubtask({ subtaskId: subtask.id, patches: { name: val } });
                  }
                }}
                className="text-[11px] font-medium text-foreground bg-transparent border-none p-0 focus:outline-none focus:ring-0 flex-1 h-auto min-w-0 truncate"
              />
            </div>

            {/* Right: Expand + Priority + Date + Delete */}
            <div className="flex items-center gap-1.5 shrink-0">
              <Button
                variant="ghost"
                size="icon"
                onClick={(e) => {
                  e.stopPropagation();
                  handleOpenSubtask(subtask.id);
                }}
                className="h-5 w-5 text-muted-foreground/40 hover:text-foreground hover:bg-muted/50 rounded shrink-0 opacity-0 group-hover:opacity-100 transition-opacity"
                title="Open subtask"
              >
                <Maximize2 className="h-3 w-3" />
              </Button>

              <PrioritySelect
                value={subtask.priority || "Low"}
                onChange={(priority) => handlePriorityChange(subtask.id, priority)}
                trigger={
                  <button type="button" className="cursor-pointer focus:outline-none bg-transparent border-none p-0">
                    <PriorityBadge priority={subtask.priority || "Low"} />
                  </button>
                }
              />

              <DateSelect
                startDate={subtask.startDate}
                dueDate={subtask.dueDate}
                onStartDateChange={(d) => handleDateChange(subtask.id, "startDate", d)}
                onDueDateChange={(d) => handleDateChange(subtask.id, "dueDate", d)}
                onClearDates={() => {
                  updateSubtask({
                    subtaskId: subtask.id,
                    patches: { clearStartDate: true, clearDueDate: true }
                  });
                }}
                size="sm"
              />

              {canCreateContent && (
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => handleDeleteSubtask(subtask.id)}
                  className="h-5 w-5 text-muted-foreground/40 hover:text-destructive hover:bg-destructive/10 rounded shrink-0 opacity-0 group-hover:opacity-100 transition-opacity"
                >
                  <Trash2 className="h-3 w-3" />
                </Button>
              )}
            </div>
          </div>
        ))}

        {canCreateContent && <div
          className="flex items-center gap-2 px-2 py-1 rounded-md border border-dashed border-border/40 hover:border-border/70 hover:bg-muted/10 transition-all cursor-text"
          onClick={() => inputRef.current?.focus()}
        >
          {/* Status picker for draft */}
          <div className="shrink-0">
            <StatusSelect
              value={draftStatusId || ""}
              onChange={setDraftStatusId}
              workflowId={parentTask?.parentWorkflowId}
            />
          </div>

          {/* Name input */}
          <input
            ref={inputRef}
            type="text"
            placeholder="Add a subtask..."
            value={draftName}
            onChange={(e) => setDraftName(e.target.value)}
            onKeyDown={handleKeyDown}
            onBlur={handleCreateSubtask}
            className="flex-1 min-w-0 text-[11px] bg-transparent border-none outline-none text-foreground placeholder:text-muted-foreground/40"
          />

          {/* Priority picker for draft */}
          <div className="shrink-0">
            <PrioritySelect
              value={draftPriority}
              onChange={setDraftPriority}
              trigger={
                <button type="button" className="cursor-pointer focus:outline-none bg-transparent border-none p-0">
                  <PriorityBadge priority={draftPriority} />
                </button>
              }
            />
          </div>

          {/* Submit hint */}
          {draftName.trim() && (
            <span className="text-[9px] text-muted-foreground/50 shrink-0">↵ to add</span>
          )}
        </div>}
      </div>

      <AlertDialog open={!!deleteSubtaskId} onOpenChange={(open) => !open && setDeleteSubtaskId(null)}>
        <AlertDialogContent className="rounded-md">
          <AlertDialogHeader>
            <AlertDialogTitle>Delete subtask</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete this subtask? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={confirmDelete}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
