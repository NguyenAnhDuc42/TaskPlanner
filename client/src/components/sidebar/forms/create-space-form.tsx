"use client"

import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { useCreateSpace } from "@/features/space/space-hooks"
import { Loader2, Plus } from "lucide-react"
import { useWorkspaceStore } from "@/utils/workspace-store"
import React from "react"

interface CreateSpaceFormProps {
  onSuccess: () => void
}

function CreateSpaceForm({ onSuccess }: CreateSpaceFormProps) {
  const { mutate, isPending, isError, error } = useCreateSpace()
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
      { workspaceId: selectedWorkspaceId, name, icon: icon || undefined },
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
        placeholder="Space name"
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
          "Create Space"
        )}
      </Button>

      {isError && (
        <div className="text-destructive text-sm mt-2">
          {error?.detail || "Failed to create space. Please try again."}
        </div>
      )}
    </form>
  )
}

interface CreateSpaceButtonProps {
  isOpen: boolean
  onOpenChange: (open: boolean) => void
  variant?: "header" | "footer"
}

export function CreateSpaceButton({ isOpen, onOpenChange, variant = "footer" }: CreateSpaceButtonProps) {
  if (variant === "header") {
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
            <DialogTitle>Create New Space</DialogTitle>
          </DialogHeader>
          <CreateSpaceForm onSuccess={() => onOpenChange(false)} />
        </DialogContent>
      </Dialog>
    )
  }

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogTrigger asChild>
        <Button
          variant="ghost"
          className="w-full justify-start text-sidebar-foreground/60 hover:bg-sidebar-accent hover:text-sidebar-foreground py-2 px-3 rounded-md border border-transparent hover:border-sidebar-border transition-all duration-200"
        >
          <div className="w-5 flex-shrink-0 flex items-center justify-center">
            <Plus className="h-4 w-4" />
          </div>
          <span className="flex-1 text-sm font-medium">New Space</span>
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Create New Space</DialogTitle>
        </DialogHeader>
        <CreateSpaceForm onSuccess={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  )
}
