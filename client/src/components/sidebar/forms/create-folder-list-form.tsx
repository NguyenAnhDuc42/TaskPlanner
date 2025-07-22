"use client"

import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { useCreateFolder } from "@/features/folder/folder-hooks"
import { useCreateList } from "@/features/list/list-hooks"
import { Loader2, Plus } from "lucide-react"
import { useWorkspaceStore } from "@/utils/workspace-store"
import React from "react"

interface CreateFolderOrListFormProps {
  spaceId: string
  onSuccess: () => void
}

function CreateFolderOrListForm({ spaceId, onSuccess }: CreateFolderOrListFormProps) {
  const [selectedType, setSelectedType] = React.useState<"folder" | "list">("folder")
  const [name, setName] = React.useState("")

  const { mutate: createFolder, isPending: isCreatingFolder } = useCreateFolder()
  const { mutate: createList, isPending: isCreatingList } = useCreateList()
  const { selectedWorkspaceId } = useWorkspaceStore()

  const isPending = isCreatingFolder || isCreatingList

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()

    if (selectedType === "folder") {
      createFolder(
        {
          workspaceId: selectedWorkspaceId!,
          spaceId: spaceId,
          name,
        },
        {
          onSuccess: () => {
            setName("")
            onSuccess()
          },
        },
      )
    } else {
      createList(
        { workspaceId: selectedWorkspaceId!, spaceId: spaceId, name },
        {
          onSuccess: () => {
            setName("")
            onSuccess()
          },
        },
      )
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3">
      <div className="flex gap-2 mb-2">
        <Button
          type="button"
          variant={selectedType === "folder" ? "default" : "outline"}
          className="flex-1"
          onClick={() => setSelectedType("folder")}
        >
          Folder
        </Button>
        <Button
          type="button"
          variant={selectedType === "list" ? "default" : "outline"}
          className="flex-1"
          onClick={() => setSelectedType("list")}
        >
          List
        </Button>
      </div>

      <input
        className="border rounded px-3 py-2"
        placeholder={`${selectedType === "folder" ? "Folder" : "List"} name`}
        value={name}
        onChange={(e) => setName(e.target.value)}
        required
      />

      <Button type="submit" disabled={isPending} className="mt-2">
        {isPending ? (
          <>
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            Creating...
          </>
        ) : (
          `Create ${selectedType === "folder" ? "Folder" : "List"}`
        )}
      </Button>
    </form>
  )
}

interface CreateFolderListButtonProps {
  spaceId: string
  spaceName: string
  isOpen: boolean
  onOpenChange: (open: boolean) => void
}

export function CreateFolderListButton({ spaceId, spaceName, isOpen, onOpenChange }: CreateFolderListButtonProps) {
  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          className="h-6 w-6 p-0 text-sidebar-foreground/60 hover:bg-sidebar-accent hover:text-sidebar-foreground transition-all duration-200"
          onClick={() => onOpenChange(true)}
        >
          <Plus className="h-3 w-3" />
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Create in {spaceName}</DialogTitle>
        </DialogHeader>
        <CreateFolderOrListForm spaceId={spaceId} onSuccess={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  )
}
