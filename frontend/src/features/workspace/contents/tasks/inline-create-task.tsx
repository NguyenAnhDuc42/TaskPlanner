import React, { useState, useRef, useEffect } from "react";
import { Plus, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useCreateTask, useTaskCreateListOptions } from "./tasks-api";
import { Priority } from "@/types/priority";
import { useListMembersAccess } from "../hierarchy/hierarchy-api";

interface InlineCreateTaskProps {
  listId?: string;
  statusId?: string;
  workspaceId: string;
  layerId: string;
  layerType: "ProjectSpace" | "ProjectFolder" | "ProjectList";
}

export function InlineCreateTask({
  listId,
  statusId,
  workspaceId,
  layerId,
  layerType,
}: InlineCreateTaskProps) {
  const [isAdding, setIsAdding] = useState(false);
  const [taskName, setTaskName] = useState("");
  const [description, setDescription] = useState("");
  const [priority, setPriority] = useState<Priority>(Priority.Normal);
  const [startDate, setStartDate] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [selectedAssigneeIds, setSelectedAssigneeIds] = useState<string[]>([]);
  const [selectedListId, setSelectedListId] = useState(listId ?? "");
  const inputRef = useRef<HTMLInputElement>(null);
  const createTask = useCreateTask(workspaceId);
  const { data: listOptions } = useTaskCreateListOptions(
    workspaceId,
    layerId,
    layerType,
    statusId,
  );
  const hasListOptions = (listOptions?.length ?? 0) > 0;
  const preferredListId = selectedListId || listId || "";
  const effectiveListId = listOptions?.some((list) => list.id === preferredListId)
    ? preferredListId
    : listOptions?.[0]?.id || "";
  const shouldShowListPicker = (listOptions?.length ?? 0) > 1;
  const { data: accessibleMembers } = useListMembersAccess(effectiveListId, false);

  const hasInvalidDateRange =
    !!startDate && !!dueDate && new Date(startDate) > new Date(dueDate);

  const toIsoDateStart = (value: string) =>
    value ? new Date(`${value}T00:00:00`).toISOString() : undefined;

  const toggleAssignee = (userId: string) => {
    setSelectedAssigneeIds((prev) =>
      prev.includes(userId)
        ? prev.filter((id) => id !== userId)
        : [...prev, userId],
    );
  };

  useEffect(() => {
    if (isAdding && inputRef.current) {
      inputRef.current.focus();
    }
  }, [isAdding]);

  const handleSubmit = async (e?: React.FormEvent) => {
    e?.preventDefault();
    if (
      !taskName.trim() ||
      createTask.isPending ||
      hasInvalidDateRange ||
      !effectiveListId
    )
      return;

    try {
      await createTask.mutateAsync({
        listId: effectiveListId,
        statusId: statusId || undefined,
        name: taskName.trim(),
        description: description.trim() || undefined,
        priority,
        startDate: toIsoDateStart(startDate),
        dueDate: toIsoDateStart(dueDate),
        assigneeIds:
          selectedAssigneeIds.length > 0 ? selectedAssigneeIds : undefined,
      });
      setTaskName("");
      setDescription("");
      setPriority(Priority.Normal);
      setStartDate("");
      setDueDate("");
      setSelectedAssigneeIds([]);
      // Keep it open for rapid entry but maybe flash success
    } catch (error) {
      console.error("Failed to create task:", error);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") {
      handleSubmit();
    } else if (e.key === "Escape") {
      setIsAdding(false);
      setTaskName("");
      setDescription("");
      setPriority(Priority.Normal);
      setStartDate("");
      setDueDate("");
      setSelectedAssigneeIds([]);
    }
  };

  if (!isAdding) {
    return (
      <Button
        variant="ghost"
        size="sm"
        className="w-full justify-start text-muted-foreground hover:text-primary hover:bg-primary/5 h-9 rounded-lg border border-dashed border-muted-foreground/20 mt-2 transition-all group"
        onClick={() => setIsAdding(true)}
      >
        <Plus className="h-4 w-4 mr-2 group-hover:scale-110 transition-transform" />
        <span className="text-xs font-medium">Add task</span>
      </Button>
    );
  }

  return (
    <div className="mt-2 p-2 rounded-xl border-2 border-primary/20 bg-background/50 backdrop-blur-sm shadow-sm ring-1 ring-primary/10 transition-all animate-in fade-in slide-in-from-top-1">
      <form onSubmit={handleSubmit} className="space-y-2">
        <Input
          ref={inputRef}
          placeholder="What needs to be done?"
          value={taskName}
          onChange={(e) => setTaskName(e.target.value)}
          onKeyDown={handleKeyDown}
          className="h-8 text-sm bg-transparent border-none focus-visible:ring-0 px-1 placeholder:text-muted-foreground/50"
          disabled={createTask.isPending}
        />
        <Input
          placeholder="Description (optional)"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          className="h-8 text-xs bg-transparent border-none focus-visible:ring-0 px-1 placeholder:text-muted-foreground/50"
          disabled={createTask.isPending}
        />
        <div className="grid grid-cols-2 gap-2 px-1">
          {!shouldShowListPicker && (
            <div className="h-8 rounded-md border bg-muted/30 px-2 text-xs flex items-center text-muted-foreground">
              {hasListOptions
                ? `${listOptions?.[0]?.icon} ${listOptions?.[0]?.name}`
                : "No list with this status"}
            </div>
          )}
          {shouldShowListPicker && (
            <select
              className="h-8 rounded-md border bg-background px-2 text-xs"
              value={effectiveListId}
              onChange={(e) => {
                setSelectedListId(e.target.value);
                setSelectedAssigneeIds([]);
              }}
              disabled={createTask.isPending || !hasListOptions}
            >
              {(listOptions ?? []).map((list) => (
                <option key={list.id} value={list.id}>
                  {list.icon} {list.name}
                </option>
              ))}
            </select>
          )}
          <div className="h-8 rounded-md border bg-muted/30 px-2 text-xs flex items-center text-muted-foreground">
            Status: {statusId ? "Current column" : "Default"}
          </div>
        </div>
        <div className="grid grid-cols-3 gap-2 px-1">
          <select
            className="h-8 rounded-md border bg-background px-2 text-xs"
            value={priority}
            onChange={(e) => setPriority(e.target.value as Priority)}
            disabled={createTask.isPending}
          >
            <option value={Priority.Low}>Low</option>
            <option value={Priority.Normal}>Normal</option>
            <option value={Priority.High}>High</option>
            <option value={Priority.Urgent}>Urgent</option>
          </select>
          <Input
            type="date"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
            className="h-8 text-xs"
            disabled={createTask.isPending}
          />
          <Input
            type="date"
            value={dueDate}
            onChange={(e) => setDueDate(e.target.value)}
            className="h-8 text-xs"
            disabled={createTask.isPending}
          />
        </div>
        {hasInvalidDateRange && (
          <div className="px-1 text-[11px] text-destructive">
            Start date cannot be later than due date.
          </div>
        )}
        {!!accessibleMembers?.length && (
          <div className="px-1">
            <div className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Assignees
            </div>
            <div className="flex flex-wrap gap-1.5">
              {accessibleMembers.map((m) => {
                const selected = selectedAssigneeIds.includes(m.userId);
                return (
                  <button
                    key={m.userId}
                    type="button"
                    onClick={() => toggleAssignee(m.userId)}
                    className={`h-6 px-2 rounded-md border text-[10px] ${
                      selected
                        ? "bg-primary/10 border-primary/40 text-primary"
                        : "bg-background border-border text-muted-foreground"
                    }`}
                  >
                    {m.userName}
                  </button>
                );
              })}
            </div>
          </div>
        )}
        <div className="flex items-center justify-between gap-2 pt-1 border-t border-muted/30">
          <div className="flex gap-1">
            {/* Future: Add quick priority/date pickers here */}
          </div>
          <div className="flex gap-1.5">
            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="h-7 w-7 rounded-md hover:bg-destructive/10 hover:text-destructive"
              onClick={() => {
                setIsAdding(false);
                setTaskName("");
                setDescription("");
                setPriority(Priority.Normal);
                setStartDate("");
                setDueDate("");
                setSelectedAssigneeIds([]);
              }}
            >
              <X className="h-3.5 w-3.5" />
            </Button>
            <Button
              type="submit"
              size="sm"
              className="h-7 px-3 rounded-md text-[11px] font-bold uppercase tracking-wider"
              disabled={
                !taskName.trim() ||
                createTask.isPending ||
                hasInvalidDateRange ||
                !effectiveListId
              }
            >
              {createTask.isPending ? "Creating..." : "Save"}
            </Button>
          </div>
        </div>
      </form>
    </div>
  );
}
