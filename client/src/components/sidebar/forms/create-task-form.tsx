"use client"

import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { useCreateTask } from "@/features/task/task-hooks"
import { Loader2, Plus } from "lucide-react"
import { useWorkspaceStore } from "@/utils/workspace-store"
import React from "react"
import { ListContext } from "../hierarchynav/list-item"

interface CreateTaskFormProps {
  listContext: ListContext
  onSuccess: () => void
}

function CreateTaskForm({ listContext, onSuccess }: CreateTaskFormProps) {
  const { mutate, isPending, isError, error } = useCreateTask()
  const { selectedWorkspaceId } = useWorkspaceStore()
  const [name, setName] = React.useState("")
  const [description, setDescription] = React.useState("")
  const [priority, setPriority] = React.useState(0)
  const [startDate, setStartDate] = React.useState("")
  const [dueDate, setDueDate] = React.useState("")
  const [isPrivate, setIsPrivate] = React.useState(false)

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!selectedWorkspaceId) {
      console.error("Workspace ID is missing. Cannot create task.")
      return
    }
    mutate(
      {
        name,
        description,
        priority,
        status: "ToDo",
        startDate: startDate ? new Date(startDate).toISOString() : null,
        dueDate: dueDate ? new Date(dueDate).toISOString() : null,
        isPrivate,

        workspaceId: selectedWorkspaceId,

        spaceId: listContext.spaceId,
        folderId: listContext.folderId,
        listId: listContext.listId,
      },
      {
        onSuccess: () => {
          setName("")
          setDescription("")
          setPriority(0)
          setStartDate("")
          setDueDate("")
          setIsPrivate(false)
          onSuccess()
        },
      },
    )
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
        ) : (
          "Create Task"
        )}
      </Button>
      {isError && (
        <div className="text-destructive text-sm mt-2">
          {error?.detail || "Failed to create task. Please try again."}
        </div>
      )}
    </form>
  )
}

interface CreateTaskButtonProps {
  listContext: ListContext
  listName: string
  isOpen: boolean
  onOpenChange: (open: boolean) => void
}

export function CreateTaskButton({ listContext, listName, isOpen, onOpenChange }: CreateTaskButtonProps) {
  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          className="h-5 w-5 p-0 text-sidebar-foreground/40 hover:bg-sidebar-accent hover:text-sidebar-foreground transition-all duration-200"
          onClick={() => onOpenChange(true)}
        >
          <Plus className="h-3 w-3" />
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Create Task in {listName}</DialogTitle>
        </DialogHeader>
        <CreateTaskForm listContext={listContext} onSuccess={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  )
}
