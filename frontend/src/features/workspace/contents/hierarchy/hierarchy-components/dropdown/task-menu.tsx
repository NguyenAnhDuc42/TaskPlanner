import {
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSub,
  DropdownMenuSubTrigger,
  DropdownMenuSubContent,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu"
import { 
  Plus,
  Trash2, 
  UserPlus, 
  Calendar,
  CircleDashed,
  CheckCircle2,
  Circle,
  Clock,
  Settings,
} from "lucide-react"
import { DialogFormWrapper } from "@/components/dialog-form-wrapper"
import { type EntityLayerType, EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type"

import { useWorkspace } from "@/features/workspace/context/workspace-provider"
import { TaskForm } from "../creation-form/task-form"
import React from "react"
import { CreateTaskForm } from "@/features/workspace/components/forms/create-task-form"

export function TaskMenu({ taskId, parentId, parentType, onAction }: { 
  taskId: string, 
  parentId: string, 
  parentType: EntityLayerType,
  onAction?: (action: string) => void 
}) {
  const { workspaceId } = useWorkspace()
  const [isCreatingTask, setIsCreatingTask] = React.useState(false)

  return (
    <>
      <DropdownMenuLabel className="font-bold text-[9px] uppercase tracking-widest opacity-50">Task Actions</DropdownMenuLabel>
      
      <DialogFormWrapper
        open={isCreatingTask}
        onOpenChange={setIsCreatingTask}
        title="Create New Task"
        trigger={
          <DropdownMenuItem className="gap-2" onSelect={(e) => { e.preventDefault(); setIsCreatingTask(true); }}>
            <Plus className="h-3.5 w-3.5" />
            <span>Create Task</span>
          </DropdownMenuItem>
        }
        contentClassName="sm:max-w-[800px] p-0 overflow-hidden border-none shadow-2xl rounded-2xl bg-background outline-none ring-1 ring-border/50"
      >
        <CreateTaskForm 
          parentId={parentId}
          parentType={parentType}
          onSuccess={() => setIsCreatingTask(false)}
          onCancel={() => setIsCreatingTask(false)}
        />
      </DialogFormWrapper>

      <DropdownMenuSeparator className="bg-border/50" />

      <DropdownMenuSub>
        <DropdownMenuSubTrigger className="gap-2">
          <CircleDashed className="h-3.5 w-3.5" />
          <span>Set Status</span>
        </DropdownMenuSubTrigger>
        <DropdownMenuSubContent className="min-w-32">
          <DropdownMenuItem className="gap-2" onClick={() => onAction?.("status-todo")}>
            <Circle className="h-3.5 w-3.5 text-muted-foreground" />
            <span>To Do</span>
          </DropdownMenuItem>
          <DropdownMenuItem className="gap-2" onClick={() => onAction?.("status-progress")}>
            <Clock className="h-3.5 w-3.5 text-blue-500" />
            <span>In Progress</span>
          </DropdownMenuItem>
          <DropdownMenuItem className="gap-2" onClick={() => onAction?.("status-done")}>
            <CheckCircle2 className="h-3.5 w-3.5 text-green-500" />
            <span>Done</span>
          </DropdownMenuItem>
        </DropdownMenuSubContent>
      </DropdownMenuSub>

      <DropdownMenuItem className="gap-2" onClick={() => onAction?.("due-date")}>
        <Calendar className="h-3.5 w-3.5" />
        <span>Set Due Date</span>
      </DropdownMenuItem>
      
      <DropdownMenuItem className="gap-2" onClick={() => onAction?.("assign")}>
        <UserPlus className="h-3.5 w-3.5" />
        <span>Assignee</span>
      </DropdownMenuItem>

      <DropdownMenuSeparator className="bg-border/50" />
      
      <DialogFormWrapper
        onOpenChange={(open) => !open && onAction?.("close-edit")}
        title="Edit Task"
        trigger={
          <DropdownMenuItem className="gap-2" onSelect={(e) => e.preventDefault()}>
            <Settings className="h-3.5 w-3.5" />
            <span>Edit Task</span>
          </DropdownMenuItem>
        }
        contentClassName="max-w-3xl p-0 overflow-hidden border-none shadow-2xl rounded-2xl bg-background outline-none ring-1 ring-border/50"
      >
        <TaskForm 
          workspaceId={workspaceId || ""}
          parentId={taskId} // Placeholder for editing task
          parentType={EntityLayerConst.ProjectFolder} // Placeholder - should actually be task context
          onSubmitSuccess={() => onAction?.("close-edit")}
          onCancel={() => onAction?.("close-edit")}
        />
      </DialogFormWrapper>

      <DropdownMenuItem variant="destructive" className="gap-2" onClick={() => onAction?.("delete")}>
        <Trash2 className="h-3.5 w-3.5" />
        <span>Delete Task</span>
      </DropdownMenuItem>
    </>
  )
}
