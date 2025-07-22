"use client"

import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { useCreateList } from "@/features/list/list-hooks"
import { Loader2, Plus } from "lucide-react"
import { useWorkspaceStore } from "@/utils/workspace-store"
import React from "react"
import { FolderContext } from "../hierarchynav/folder-item"


interface CreateListFormProps {
  folderContext: FolderContext
  onSuccess: () => void
}

function CreateListForm({ folderContext, onSuccess }: CreateListFormProps) {
  const { mutate, isPending, isError, error } = useCreateList()
  const [name, setName] = React.useState("")
  const [icon, setIcon] = React.useState("")
  const { selectedWorkspaceId } = useWorkspaceStore()

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()

    if (!selectedWorkspaceId) {
      console.error("Workspace ID is missing")
      return
    }

    mutate(
      {
        workspaceId: selectedWorkspaceId,
        spaceId: folderContext.spaceId,
        folderId: folderContext.folderId,
        name,
        icon: icon || undefined,
      },
      {
        onSuccess: () => {
          setName("")
          setIcon("")
          onSuccess()
        },
      },
    )
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3">
      <input
        className="border rounded px-3 py-2"
        placeholder="List name"
        value={name}
        onChange={(e) => setName(e.target.value)}
        required
      />
      <input
        className="border rounded px-3 py-2"
        placeholder="Icon URL (optional)"
        value={icon}
        onChange={(e) => setIcon(e.target.value)}
      />
      <Button type="submit" disabled={isPending} className="mt-2">
        {isPending ? (
          <>
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            Creating...
          </>
        ) : (
          "Create List"
        )}
      </Button>
      {isError && (
        <div className="text-destructive text-sm mt-2">
          {error?.detail || "Failed to create list. Please try again."}
        </div>
      )}
    </form>
  )
}

interface CreateListButtonProps {
  folderContext: FolderContext
  folderName: string
  isOpen: boolean
  onOpenChange: (open: boolean) => void
}

export function CreateListButton({ folderContext, folderName, isOpen, onOpenChange }: CreateListButtonProps) {
  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          className="h-5 w-5 p-0 text-sidebar-foreground/50 hover:bg-sidebar-accent hover:text-sidebar-foreground transition-all duration-200"
          onClick={() => onOpenChange(true)}
        >
          <Plus className="h-3 w-3" />
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Create List in {folderName}</DialogTitle>
        </DialogHeader>
        <CreateListForm folderContext={folderContext} onSuccess={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  )
}
