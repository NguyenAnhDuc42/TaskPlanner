import {
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu"
import { 
  Plus, 
  Trash2,
  Calendar,
  Type
} from "lucide-react"
import { DialogFormWrapper } from "@/components/dialog-form-wrapper"

import { useWorkspace } from "@/features/workspace/context/workspace-provider"
import { useState } from "react"
import { EntityLayerType } from "@/types/entity-layer-type"
import { CreateTaskForm } from "@/features/workspace/components/forms/create-task-form"
import { CreateFolderForm } from "@/features/workspace/components/forms/create-folder-form"
import { FolderForm } from "../creation-form/folder-form"

export function FolderMenu({ folderId, spaceId, onAction }: { folderId: string, spaceId: string, onAction?: (action: string) => void }) {
  const { workspaceId } = useWorkspace()
  const [activeForm, setActiveForm] = useState<"folder" | "task" | null>(null)

  return (
    <>
      <DropdownMenuLabel className="font-bold text-[9px] uppercase tracking-widest opacity-50">Folder Actions</DropdownMenuLabel>
      
      <DialogFormWrapper
        open={activeForm === "folder"}
        onOpenChange={(open) => !open && setActiveForm(null)}
        title="Create New Folder"
        trigger={
          <DropdownMenuItem className="gap-2" onSelect={(e) => { e.preventDefault(); setActiveForm("folder"); }}>
            <Plus className="h-3.5 w-3.5" />
            <span>Create Folder</span>
          </DropdownMenuItem>
        }
      >
        <CreateFolderForm 
          spaceId={spaceId}
          onSuccess={() => setActiveForm(null)}
          onCancel={() => setActiveForm(null)}
        />
      </DialogFormWrapper>
      <DialogFormWrapper
        open={activeForm === "task"}
        onOpenChange={(open) => !open && setActiveForm(null)}
        title="Create New Task"
        trigger={
          <DropdownMenuItem className="gap-2" onSelect={(e) => { e.preventDefault(); setActiveForm("task"); }}>
            <Plus className="h-3.5 w-3.5" />
            <span>Create Task</span>
          </DropdownMenuItem>
        }
      >
        <CreateTaskForm 
          parentId={folderId}
          parentType={EntityLayerType.ProjectFolder}
          onSuccess={() => setActiveForm(null)}
          onCancel={() => setActiveForm(null)}
        />
      </DialogFormWrapper>

      <DropdownMenuItem className="gap-2" onClick={() => onAction?.("date-time")}>
        <Calendar className="h-3.5 w-3.5" />
        <span>Set Date & Time</span>
      </DropdownMenuItem>
      
      <DropdownMenuSeparator className="bg-border/50" />
      
      <DialogFormWrapper
        onOpenChange={(open) => !open && onAction?.("close-rename")}
        title="Rename Folder"
        trigger={
          <DropdownMenuItem className="gap-2" onSelect={(e) => e.preventDefault()}>
            <Type className="h-3.5 w-3.5" />
            <span>Rename Folder</span>
          </DropdownMenuItem>
        }
        contentClassName="sm:max-w-[800px] p-0 overflow-hidden border-none shadow-2xl rounded-2xl bg-background outline-none ring-1 ring-border/50"
      >
        <FolderForm 
          workspaceId={workspaceId || ""}
          spaceId={spaceId}
          onSubmitSuccess={() => onAction?.("close-rename")}
          onCancel={() => onAction?.("close-rename")}
        />
      </DialogFormWrapper>
      
      <DropdownMenuItem variant="destructive" className="gap-2" onClick={() => onAction?.("delete")}>
        <Trash2 className="h-3.5 w-3.5" />
        <span>Delete Folder</span>
      </DropdownMenuItem>
    </>
  )
}
