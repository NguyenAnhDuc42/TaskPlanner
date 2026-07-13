import { useRef, useState, useMemo } from "react";
import { observer } from "mobx-react-lite";
import { Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { StatusSelect } from "@/components/status-select";
import { PrioritySelect } from "@/components/priority-select";
import { PriorityBadge } from "@/components/priority-badge";
import { DateSelect } from "@/components/date-select";
import { DebouncedInput } from "@/components/debounced-input";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { Priority } from "@/types/priority";
import type { TaskRecord } from "@/types/projects/task-record";
import { useNavigate, useLocation } from "@tanstack/react-router";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { useDebouncedFlush } from "@/sync/use-debounced-flush";
import { TaskMutations } from "@/mutations/task.mutations";
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

export const TaskSubtasks = observer(function TaskSubtasks({ taskId }: Readonly<TaskSubtasksProps>) {
  const { canCreateContent } = useWorkspaceRole();
  const navigate = useNavigate();
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const taskMutations = useMemo(() => new TaskMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const { scheduleFlush } = useDebouncedFlush(syncEngine);

  const subtasks = rootStore.taskStore.getSubTask(taskId);
  const parentTask = rootStore.taskStore.getById(taskId);

  const updateSubtask = (subtaskId: string, patches: Partial<TaskRecord>) => {
    taskMutations.updateLocal(subtaskId, patches).catch((err) => console.error("Failed to apply local subtask update", err));
    scheduleFlush();
  };

  const deleteSubtask = (subtaskId: string) => {
    taskMutations.delete(subtaskId).catch((err) => console.error("Failed to delete subtask", err));
  };

  // New subtask draft state
  const [draftName, setDraftName] = useState("");
  const [draftStatusId, setDraftStatusId] = useState<string | null | undefined>(undefined);
  const [draftPriority, setDraftPriority] = useState<Priority>("Low");
  const [deleteSubtaskId, setDeleteSubtaskId] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleCreateSubtask = () => {
    if (!draftName.trim() || !parentTask) return;
    const name = draftName.trim();
    const priority = draftPriority;
    const statusId = draftStatusId;

   
    setDraftName("");
    setDraftStatusId(undefined);
    setDraftPriority(Priority.None);

    taskMutations.create({
      name,
      priority,
      statusId,
      spaceId: parentTask.spaceId,
      folderId: parentTask.folderId,
      parentTaskId: taskId,
    }).catch((err) => console.error("Failed to create subtask", err));
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      e.preventDefault();
      handleCreateSubtask();
    }
  };

  const handleStatusChange = (subtaskId: string, statusId: string | null) => {
    updateSubtask(subtaskId, { statusId });
  };

  const handlePriorityChange = (subtaskId: string, priority: Priority) => {
    updateSubtask(subtaskId, { priority });
  };

  const handleDateChange = (subtaskId: string, field: "startDate" | "dueDate", date: Date | undefined) => {
    updateSubtask(subtaskId, { [field]: date ? date.toISOString() : null });
  };

  const handleDeleteSubtask = (subtaskId: string) => {
    setDeleteSubtaskId(subtaskId);
  };

  const confirmDelete = () => {
    if (deleteSubtaskId) {
      deleteSubtask(deleteSubtaskId);
      setDeleteSubtaskId(null);
    }
  };

  const location = useLocation();
  const handleOpenSubtask = (subtaskId: string) => {
    const fullTaskPageMatch = /^\/workspaces\/([^/]+)\/tasks\/[^/]+$/.exec(location.pathname);
    if (fullTaskPageMatch) {
      navigate({
        to: "/workspaces/$workspaceId/tasks/$taskId",
        params: { workspaceId: fullTaskPageMatch[1], taskId: subtaskId },
      });
      return;
    }

    navigate({
      to: location.pathname,
      search: {
        ...(location.search as Record<string, unknown>),
        contextPanel: { type: "task", id: subtaskId },
      },
    });
  };

  return (
    <div className="space-y-3">
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
                  spaceId={parentTask?.spaceId ?? undefined}
                />
              </div>
              <DebouncedInput
                value={subtask.name}
                onChange={(val) => {
                  if (val.trim() && val !== subtask.name) {
                    updateSubtask(subtask.id, { name: val });
                  }
                }}
                className="text-[11px] font-medium text-foreground bg-transparent border-none p-0 focus:outline-none focus:ring-0 flex-1 h-5 leading-5 min-w-0 truncate"
              />
            </div>

            {/* Right: Priority + Date + Delete */}
            <div className="flex items-center gap-1.5 shrink-0">
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
                  updateSubtask(subtask.id, { startDate: null, dueDate: null });
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
              spaceId={parentTask?.spaceId ?? undefined}
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
            className="flex-1 min-w-0 h-5 leading-5 text-[11px] bg-transparent border-none outline-none text-foreground placeholder:text-muted-foreground/40"
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
});
