import { useState } from "react";
import { useSelector } from "react-redux";
import { Plus, Trash2, Calendar, CheckSquare } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { taskSelectors } from "@/store/entityStore";
import type { RootState } from "@/store";
import { useCreateSubTaskMutation, useUpdateTaskMutation } from "../task-api";
import { StatusSelect } from "@/components/status-select";
import { PrioritySelect } from "@/components/priority-select";
import { PriorityBadge } from "@/components/priority-badge";
import { DateSelect } from "@/components/date-select";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { useDeleteTaskMutation } from "../../../hierarchy/hierarchy-api";

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
  const [updateTask] = useUpdateTaskMutation();
  const [deleteTask] = useDeleteTaskMutation();

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
    updateTask({ taskId: subtaskId, patches: { statusId } });
  };

  const handlePriorityChange = (subtaskId: string, priority: any) => {
    updateTask({ taskId: subtaskId, patches: { priority } });
  };

  const handleDateChange = (subtaskId: string, field: "startDate" | "dueDate", date: Date | undefined) => {
    updateTask({
      taskId: subtaskId,
      patches: { [field]: date ? date.toISOString() : undefined },
    });
  };

  const handleDeleteSubtask = (subtaskId: string) => {
    if (confirm("Are you sure you want to delete this subtask?")) {
      deleteTask({ workspaceId: workspaceId || "", taskId: subtaskId });
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
            className="flex flex-wrap items-center justify-between gap-3 p-2 rounded-lg border border-border/30 bg-muted/5 hover:bg-muted/10 transition-colors"
          >
            <div className="flex items-center gap-2.5 flex-1 min-w-[200px]">
              <StatusSelect
                value={subtask.statusId || ""}
                onChange={(statusId) => handleStatusChange(subtask.id, statusId)}
                workflowId={subtask.parentWorkflowId}
              />
              <input
                value={subtask.name}
                onChange={(e) =>
                  updateTask({ taskId: subtask.id, patches: { name: e.target.value } })
                }
                className="text-xs font-medium text-foreground bg-transparent border-none p-0 focus:outline-none focus:ring-0 flex-1"
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
          <form onSubmit={handleAddSubtask} className="flex gap-2 items-center p-2 rounded-lg border border-primary/20 bg-primary/5">
            <Input
              placeholder="Subtask name..."
              value={newSubtaskName}
              onChange={(e) => setNewSubtaskName(e.target.value)}
              className="text-xs h-8 bg-muted/20 border-none flex-1 focus-visible:ring-1"
              autoFocus
            />
            <div className="flex items-center gap-1.5">
              <Button type="submit" size="sm" className="h-8 text-xs px-3" disabled={isCreating || !newSubtaskName.trim()}>
                Create
              </Button>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => {
                  setIsAdding(false);
                  setNewSubtaskName("");
                }}
                className="h-8 text-xs px-3"
              >
                Cancel
              </Button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
