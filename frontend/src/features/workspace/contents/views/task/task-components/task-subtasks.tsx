import { useState } from "react";
import { useSelector } from "react-redux";
import { Plus, Trash2, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { taskSelectors } from "@/store/entityStore";
import type { RootState } from "@/store";
import { 
  useCreateSubTaskMutation, 
  useUpdateTaskMutation, 
  type UpdateTaskPayload 
} from "../task-api";
import { StatusSelect } from "@/components/status-select";
import { PrioritySelect } from "@/components/priority-select";
import { PriorityBadge } from "@/components/priority-badge";
import { DateSelect } from "@/components/date-select";
import { DebouncedInput } from "@/components/debounced-input";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { useDeleteTaskMutation } from "../../../hierarchy/hierarchy-api";
import type { Priority } from "@/types/priority";

interface TaskSubtasksProps {
  taskId: string;
}

export function TaskSubtasks({ taskId }: Readonly<TaskSubtasksProps>) {
  const { workspaceId } = useWorkspace();
  const subtasks = useSelector((state: RootState) =>
    taskSelectors.selectAll(state).filter((t) => t.parentTaskId === taskId)
  );

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

  const [newSubtaskName, setNewSubtaskName] = useState("");
  const [isAdding, setIsAdding] = useState(false);

  const handleAddSubtask = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newSubtaskName.trim()) return;

    try {
      await createSubTask({
        spaceId: parentTask?.spaceId || "",
        parentTaskId: taskId,
        name: newSubtaskName.trim(),
        priority: "Low",
      }).unwrap();
      setNewSubtaskName("");
      setIsAdding(false);
    } catch (err) {
      console.error("Failed to create subtask", err);
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
    if (confirm("Are you sure you want to delete this subtask?")) {
      deleteSubtask({ subtaskId });
    }
  };

  return (
    <div className="space-y-4 pt-6 border-t border-border/30">
      <div className="flex items-center justify-between">
        <h3 className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground/70">
          Subtasks
        </h3>
        {!isAdding && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setIsAdding(true)}
            className="h-6 px-2 text-[10px] text-muted-foreground hover:text-foreground hover:bg-muted/50 rounded-md"
          >
            <Plus className="h-3 w-3 mr-1" /> Add Subtask
          </Button>
        )}
      </div>

      <div className="space-y-2">
        {subtasks.map((subtask) => (
          <div
            key={subtask.id}
            className="group flex items-center justify-between gap-4 p-2.5 rounded-md hover:bg-muted/30 border border-transparent hover:border-border/30 transition-all"
          >
            <div className="flex items-center gap-3 flex-1 min-w-[200px]">
              <div className="pr-3 border-r border-border/40 shrink-0 h-6 flex items-center">
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
                className="text-xs font-medium text-foreground bg-transparent border-none p-0 focus:outline-none focus:ring-0 flex-1 h-auto"
              />
            </div>

            <div className="flex items-center gap-2">
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
                size="sm"
              />

              <Button
                variant="ghost"
                size="icon"
                onClick={() => handleDeleteSubtask(subtask.id)}
                className="h-7 w-7 text-muted-foreground hover:text-destructive hover:bg-destructive/10 rounded-md shrink-0"
              >
                <Trash2 className="h-3.5 w-3.5" />
              </Button>
            </div>
          </div>
        ))}

        {subtasks.length === 0 && !isAdding && (
          <p className="text-xs text-muted-foreground/50 italic py-2">No subtasks yet.</p>
        )}

        {isAdding && (
          <form onSubmit={handleAddSubtask} className="flex gap-2 items-center p-1.5 rounded-md border border-border/40 bg-muted/20 mt-1">
            <Input
              placeholder="What needs to be done?"
              value={newSubtaskName}
              onChange={(e) => setNewSubtaskName(e.target.value)}
              className="text-xs h-8 bg-transparent border-none flex-1 focus-visible:ring-0 px-2"
              autoFocus
            />
            <div className="flex items-center gap-1 pr-0.5">
              <Button type="submit" size="sm" className="h-7 text-[10px] font-semibold px-3 rounded-md" disabled={isCreating || !newSubtaskName.trim()}>
                Add
              </Button>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                onClick={() => {
                  setIsAdding(false);
                  setNewSubtaskName("");
                }}
                className="h-7 w-7 rounded-md text-muted-foreground hover:bg-muted hover:text-foreground"
              >
                <X className="h-3.5 w-3.5" />
              </Button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
