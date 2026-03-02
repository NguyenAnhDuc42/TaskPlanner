import React, { useState, useRef, useEffect } from "react";
import { Plus, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useCreateTask } from "./tasks-api";
import { Priority } from "@/types/priority";

interface InlineCreateTaskProps {
  listId: string;
  statusId?: string;
  workspaceId: string;
}

export function InlineCreateTask({
  listId,
  statusId,
  workspaceId,
}: InlineCreateTaskProps) {
  const [isAdding, setIsAdding] = useState(false);
  const [taskName, setTaskName] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);
  const createTask = useCreateTask(workspaceId);

  useEffect(() => {
    if (isAdding && inputRef.current) {
      inputRef.current.focus();
    }
  }, [isAdding]);

  const handleSubmit = async (e?: React.FormEvent) => {
    e?.preventDefault();
    if (!taskName.trim() || createTask.isPending) return;

    try {
      await createTask.mutateAsync({
        listId,
        statusId,
        name: taskName.trim(),
        priority: Priority.Normal,
      });
      setTaskName("");
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
              }}
            >
              <X className="h-3.5 w-3.5" />
            </Button>
            <Button
              type="submit"
              size="sm"
              className="h-7 px-3 rounded-md text-[11px] font-bold uppercase tracking-wider"
              disabled={!taskName.trim() || createTask.isPending}
            >
              {createTask.isPending ? "Creating..." : "Save"}
            </Button>
          </div>
        </div>
      </form>
    </div>
  );
}
