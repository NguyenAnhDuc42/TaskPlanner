import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader2 } from "lucide-react";
import { Priority } from "@/types/priority";
import { useCreateTask, useTaskCreateListOptions } from "../tasks-api";
import { toast } from "sonner";
import { useStatuses } from "../../hierarchy/statuses-api";

interface CreateTaskFormProps {
  workspaceId: string;
  layerId: string;
  layerType: string;
  onSuccess?: () => void;
}

export function CreateTaskForm({
  workspaceId,
  layerId,
  layerType,
  onSuccess,
}: CreateTaskFormProps) {
  const { data: listOptions, isLoading: isLoadingLists } =
    useTaskCreateListOptions(workspaceId, layerId, layerType);
  const createTask = useCreateTask(workspaceId);

  const [selectedListId, setSelectedListId] = useState("");
  const [selectedStatusId, setSelectedStatusId] = useState("");
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [priority, setPriority] = useState<Priority>(Priority.Normal);
  const effectiveListId = selectedListId || listOptions?.[0]?.id || "";
  const { data: listStatuses, isLoading: isLoadingStatuses } = useStatuses(
    effectiveListId,
    "ProjectList",
  );
  const effectiveStatusId =
    (selectedStatusId &&
    listStatuses?.some((s) => s.id === selectedStatusId)
      ? selectedStatusId
      : listStatuses?.find((s) => s.isDefault)?.id || listStatuses?.[0]?.id) ||
    "";

  const isSubmitting = createTask.isPending;
  const hasListOptions = (listOptions?.length ?? 0) > 0;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim() || !effectiveListId || isSubmitting) return;

    try {
      await createTask.mutateAsync({
        listId: effectiveListId,
        statusId: effectiveStatusId || undefined,
        name: name.trim(),
        description: description.trim() || undefined,
        priority,
      });

      toast.success("Task created successfully");
      setName("");
      setDescription("");
      setPriority(Priority.Normal);
      onSuccess?.();
    } catch (error: unknown) {
      const message =
        error instanceof Error ? error.message : "Failed to create task";
      toast.error(message);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="task-list">List</Label>
        <select
          id="task-list"
          className="w-full h-9 rounded-md border bg-background px-3 text-sm"
          value={effectiveListId}
          onChange={(e) => {
            setSelectedListId(e.target.value);
            setSelectedStatusId("");
          }}
          disabled={isLoadingLists || isSubmitting || !hasListOptions}
        >
          {isLoadingLists && <option>Loading lists...</option>}
          {!isLoadingLists && !hasListOptions && (
            <option>No accessible lists</option>
          )}
          {!isLoadingLists &&
            listOptions?.map((list) => (
              <option key={list.id} value={list.id}>
                {list.icon} {list.name}
              </option>
            ))}
        </select>
      </div>

      <div className="space-y-2">
        <Label htmlFor="task-status">Status</Label>
        <select
          id="task-status"
          className="w-full h-9 rounded-md border bg-background px-3 text-sm"
          value={effectiveStatusId}
          onChange={(e) => setSelectedStatusId(e.target.value)}
          disabled={isLoadingStatuses || isSubmitting || !listStatuses?.length}
        >
          {isLoadingStatuses && <option>Loading statuses...</option>}
          {!isLoadingStatuses && !listStatuses?.length && <option>No status</option>}
          {!isLoadingStatuses &&
            listStatuses?.map((status) => (
              <option key={status.id} value={status.id}>
                {status.name}
              </option>
            ))}
        </select>
      </div>

      <div className="space-y-2">
        <Label htmlFor="task-name">Task Name</Label>
        <Input
          id="task-name"
          placeholder="Enter task name..."
          value={name}
          onChange={(e) => setName(e.target.value)}
          disabled={isSubmitting}
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="task-description">Description</Label>
        <Input
          id="task-description"
          placeholder="Optional description..."
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          disabled={isSubmitting}
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="task-priority">Priority</Label>
        <select
          id="task-priority"
          className="w-full h-9 rounded-md border bg-background px-3 text-sm"
          value={priority}
          onChange={(e) => setPriority(e.target.value as Priority)}
          disabled={isSubmitting}
        >
          <option value={Priority.Low}>Low</option>
          <option value={Priority.Normal}>Normal</option>
          <option value={Priority.High}>High</option>
          <option value={Priority.Urgent}>Urgent</option>
        </select>
      </div>

      {!hasListOptions && !isLoadingLists && (
        <p className="text-xs text-muted-foreground">
          No accessible lists in this layer for current user.
        </p>
      )}

      <div className="flex justify-end">
        <Button
          type="submit"
          disabled={
            !name.trim() ||
            !effectiveListId ||
            isSubmitting ||
            !hasListOptions
          }
        >
          {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          Create Task
        </Button>
      </div>
    </form>
  );
}
