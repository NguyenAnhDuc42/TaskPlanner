import { Button } from "@/components/ui/button";
import { ListNode } from "@/features/workspace/workspacetype";
import { cn } from "@/lib/utils";
import { List, MoreHorizontal, Plus } from "lucide-react";
import React from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { useCreateTask } from "@/features/task/task-hooks";
import { Loader2 } from "lucide-react";
import { useWorkspaceStore } from "@/utils/workspace-store";
import Link from "next/link";
export type ListContext = {
  spaceId: string;
  folderId?: string;
  listId: string;
}

interface ListItemProps {
  list: ListNode;
  context: ListContext;
  isHighlighted?: boolean;
}

interface CreateTaskFormProps {
  listContext: ListContext;
  onSuccess: () => void;
}

function CreateTaskForm({ listContext, onSuccess }: CreateTaskFormProps) {
  const { mutate, isPending, isError, error } = useCreateTask();
  const { selectedWorkspaceId } = useWorkspaceStore();
  const [name, setName] = React.useState("");
  const [description, setDescription] = React.useState("");
  const [priority, setPriority] = React.useState(0);
  const [startDate, setStartDate] = React.useState("");
  const [dueDate, setDueDate] = React.useState("");
  const [isPrivate, setIsPrivate] = React.useState(false);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!selectedWorkspaceId) {
      console.error("Workspace ID is missing. Cannot create task.");
      return;
    }
    mutate(
      { 
        name,
        description,
        priority,
        status: "ToDo",
        startDate: startDate
          ? new Date(startDate).toISOString()
          : null,
        dueDate: dueDate
          ? new Date(dueDate).toISOString()
          : null,
        isPrivate,

        workspaceId: selectedWorkspaceId,

        spaceId: listContext.spaceId,
        folderId: listContext.folderId,
        listId: listContext.listId,
      },
      {
        onSuccess: () => {
          setName("");
          setDescription("");
          setPriority(0);
          setStartDate("");
          setDueDate("");
          setIsPrivate(false);
          onSuccess();
        },
      }
    );
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3">
      <input
        className="border rounded px-3 py-2"
        placeholder="Task name"
        value={name}
        onChange={(e) => setName(e.target.value)}
        required
      />
      <textarea
        className="border rounded px-3 py-2"
        placeholder="Description (optional)"
        value={description}
        onChange={(e) => setDescription(e.target.value)}
      />
      <input
        type="number"
        min="0"
        max="10"
        className="border rounded px-3 py-2"
        placeholder="Priority (0-10)"
        value={priority}
        onChange={(e) => setPriority(Number(e.target.value))}
      />
      <div className="grid grid-cols-2 gap-2">
        <div>
          <label className="block text-sm font-medium mb-1">Start Date</label>
          <input
            type="date"
            className="border rounded px-3 py-2 w-full"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
          />
        </div>
        <div>
          <label className="block text-sm font-medium mb-1">Due Date</label>
          <input
            type="date"
            className="border rounded px-3 py-2 w-full"
            value={dueDate}
            onChange={(e) => setDueDate(e.target.value)}
          />
        </div>
      </div>
      <label className="flex items-center gap-2 mt-2">
        <input
          type="checkbox"
          checked={isPrivate}
          onChange={(e) => setIsPrivate(e.target.checked)}
          className="size-4"
        />
        Private task
      </label>
      <Button type="submit" disabled={isPending} className="mt-2">
        {isPending ? (
          <>
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            Creating...
          </>
        ) : "Create Task"}
      </Button>
      {isError && (
        <div className="text-destructive text-sm mt-2">
          {error?.detail || "Failed to create task. Please try again."}
        </div>
      )}
    </form>
  );
}

export function ListItem({ list, context, isHighlighted }: ListItemProps) {
  const { selectedWorkspaceId } = useWorkspaceStore();
  const [showCreateTaskModal, setShowCreateTaskModal] = React.useState(false);
   const listUrl = selectedWorkspaceId
    ? `/ws/${selectedWorkspaceId}/l/${context.listId}`
    : '#'; 
  
  return (
    <div className={cn("group flex items-center gap-2 py-1 px-2 rounded-sm cursor-pointer","hover:bg-neutral-800", isHighlighted && "bg-neutral-800",)}>
      <div className="w-6 flex-shrink-0 flex items-center justify-center">
        <List className="h-4 w-4 text-gray-400" />
      </div>
       <Link href={listUrl} className="flex flex-1 items-center gap-2 min-w-0" passHref>
        <span className="flex-1 text-sm text-neutral-200 truncate">{list.name}</span>
      </Link>
      <div className="flex items-center gap-1">
        <Button variant="ghost" size="sm" className="h-6 w-6 p-0 text-neutral-400 hover:bg-neutral-700">
          <MoreHorizontal className="h-4 w-4" />
        </Button>
        
        <Dialog open={showCreateTaskModal} onOpenChange={setShowCreateTaskModal}>
          <DialogTrigger asChild>
            <Button 
              variant="ghost" 
              size="sm" 
              className="h-6 w-6 p-0 text-neutral-400 hover:bg-neutral-700"
              onClick={() => setShowCreateTaskModal(true)}
            >
              <Plus className="h-4 w-4" />
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Create Task in {list.name}</DialogTitle>
            </DialogHeader>
            <CreateTaskForm 
              listContext={context} 
              onSuccess={() => setShowCreateTaskModal(false)} 
            />
          </DialogContent>
        </Dialog>
      </div>
    </div>
  )
}