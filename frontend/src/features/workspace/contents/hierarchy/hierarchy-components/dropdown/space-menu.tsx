import {
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu"
import { 
  Plus, 
  FolderPlus, 
  Settings, 
  Trash2,
  Users
} from "lucide-react"
import { DialogFormWrapper } from "@/components/dialog-form-wrapper"
import { SpaceForm } from "../creation-form/space-form"
import { FolderForm } from "../creation-form/folder-form"
import { TaskForm } from "../creation-form/task-form"
import { useSidebarContext } from "@/features/workspace/components/sidebar-provider"
import { useState } from "react"

export function SpaceMenu({ spaceId, onAction }: { spaceId: string, onAction?: (action: string) => void }) {
  const { workspaceId } = useSidebarContext()
  const [activeForm, setActiveForm] = useState<"space" | "folder" | "task" | null>(null)

  return (
    <>
      <DropdownMenuLabel className="font-bold text-[9px] uppercase tracking-widest opacity-50">Space Actions</DropdownMenuLabel>
      
      <DialogFormWrapper
        open={activeForm === "space"}
        onOpenChange={(open) => !open && setActiveForm(null)}
        title="Create New Space"
        trigger={
          <DropdownMenuItem className="gap-2" onSelect={(e) => { e.preventDefault(); setActiveForm("space"); }}>
            <Plus className="h-3.5 w-3.5" />
            <span>Create Space</span>
          </DropdownMenuItem>
        }
        contentClassName="sm:max-w-[800px] p-0 overflow-hidden border-none shadow-2xl rounded-2xl bg-background outline-none ring-1 ring-border/50"
      >
        <SpaceForm 
          workspaceId={workspaceId || ""}
          onSubmitSuccess={() => setActiveForm(null)}
          onCancel={() => setActiveForm(null)}
        />
      </DialogFormWrapper>
      <DialogFormWrapper
        open={activeForm === "folder"}
        onOpenChange={(open) => !open && setActiveForm(null)}
        title="Create New Folder"
        trigger={
          <DropdownMenuItem className="gap-2" onSelect={(e) => { e.preventDefault(); setActiveForm("folder"); }}>
            <FolderPlus className="h-3.5 w-3.5" />
            <span>Create Folder</span>
          </DropdownMenuItem>
        }
        contentClassName="sm:max-w-[800px] p-0 overflow-hidden border-none shadow-2xl rounded-2xl bg-background outline-none ring-1 ring-border/50"
      >
        <FolderForm 
          workspaceId={workspaceId || ""}
          spaceId={spaceId}
          onSubmitSuccess={() => setActiveForm(null)}
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
        contentClassName="max-w-3xl p-0 overflow-hidden border-none shadow-2xl rounded-2xl bg-background outline-none ring-1 ring-border/50"
      >
        <TaskForm 
          workspaceId={workspaceId || ""}
          parentId={spaceId}
          parentType="Space"
          onSubmitSuccess={() => setActiveForm(null)}
          onCancel={() => setActiveForm(null)}
        />
      </DialogFormWrapper>

      <DropdownMenuSeparator className="bg-border/50" />
      
      <DropdownMenuItem className="gap-2" onClick={() => onAction?.("members")}>
        <Users className="h-3.5 w-3.5" />
        <span>Manage Members</span>
      </DropdownMenuItem>
      
      <DialogFormWrapper
        onOpenChange={(open) => !open && onAction?.("close-settings")}
        title="Space Settings"
        trigger={
          <DropdownMenuItem className="gap-2" onSelect={(e) => e.preventDefault()}>
            <Settings className="h-3.5 w-3.5" />
            <span>Space Settings</span>
          </DropdownMenuItem>
        }
        contentClassName="max-w-3xl p-0 overflow-hidden border-none shadow-2xl rounded-2xl bg-background outline-none ring-1 ring-border/50"
      >
        <SpaceForm 
          workspaceId={workspaceId || ""}
          onSubmitSuccess={() => onAction?.("close-settings")}
          onCancel={() => onAction?.("close-settings")}
        />
      </DialogFormWrapper>
      
      <DropdownMenuItem variant="destructive" className="gap-2" onClick={() => onAction?.("delete")}>
        <Trash2 className="h-3.5 w-3.5" />
        <span>Delete Space</span>
      </DropdownMenuItem>
    </>
  )
}
