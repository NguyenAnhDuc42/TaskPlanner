"use client"

import * as React from "react"
import { Plus } from "lucide-react"
import { Button } from "@/components/ui/button"
import { SpaceNode } from "@/features/workspace/workspacetype"
import { SpaceItem } from "./space-item"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { useCreateSpace } from "@/features/space/space-hooks"
import { Loader2 } from "lucide-react"
import { useWorkspaceStore } from "@/utils/workspace-store"

export function SpaceHierarchyDisplay({ spaces }: { spaces: SpaceNode[] }) {
  const [isCreateSpaceModalOpen, setIsCreateSpaceModalOpen] = React.useState(false);
  
  return (
    <div className="px-2 space-y-1">
      {spaces.map((space) => (
        <SpaceItem key={space.id} space={space} context={{ spaceId: space.id }} />
      ))}
      
      <Dialog open={isCreateSpaceModalOpen} onOpenChange={setIsCreateSpaceModalOpen}>
        <DialogTrigger asChild>
          <Button 
            variant="ghost" 
            className="w-full justify-start mt-4 text-neutral-400 hover:bg-neutral-800 hover:text-neutral-200 py-1 px-2"
          >
            <div className="w-6 flex-shrink-0 flex items-center justify-center">
              <Plus className="h-4 w-4" />
            </div>
            <span className="flex-1 text-sm font-medium">New Space</span>
          </Button>
        </DialogTrigger>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create New Space</DialogTitle>
          </DialogHeader>
          <CreateSpaceForm onSuccess={() => setIsCreateSpaceModalOpen(false)} />
        </DialogContent>
      </Dialog>
    </div>
  )
}

interface CreateSpaceFormProps {
  onSuccess: () => void;
}

function CreateSpaceForm({ onSuccess }: CreateSpaceFormProps) {
  const { mutate, isPending, isError, error } = useCreateSpace();
  const [name, setName] = React.useState("");
  const [icon, setIcon] = React.useState("");
  const { selectedWorkspaceId } = useWorkspaceStore();

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    
    if (!selectedWorkspaceId) {
      console.error("Workspace ID is missing");
      return;
    }
    
    mutate( {  workspaceId: selectedWorkspaceId,  name, icon: icon || undefined },
      {
        onSuccess: () => {
          setName("");
          setIcon("");
          onSuccess();
        },
      }
    );
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3">
      <input className="border rounded px-3 py-2" placeholder="Space name"
        value={name}  onChange={(e) => setName(e.target.value)} required />

      <input className="border rounded px-3 py-2" placeholder="Icon URL (optional)"
        value={icon} onChange={(e) => setIcon(e.target.value)} />
      <Button type="submit" disabled={isPending} className="mt-2">
        {isPending ? (
          <>
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            Creating...
          </>
        ) : "Create Space"}
      </Button>
      {isError && (
        <div className="text-destructive text-sm mt-2">
          {error?.detail || "Failed to create space. Please try again."}
        </div>
      )}
    </form>
  );
}