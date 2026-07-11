import { useMemo } from "react";
import { observer } from "mobx-react-lite";
import { EntityViewFrame } from "../entity-view-frame";
import { TaskDetailCanvas, useDebouncedTaskUpdate } from "./components/task-detail-canvas";
import { TaskPropertiesPanel } from "./components/task-properties-panel";
import { useNavigate, useParams } from "@tanstack/react-router";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import { MoreVertical, Trash2, PanelRight, X } from "lucide-react";
import type { Priority } from "@/types/priority";
import { FavoriteButton } from "@/components/favorite-button";
import { EntityLayerType } from "@/types/entity-layer-type";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { Link } from "@tanstack/react-router";
import { DynamicIcon } from "@/components/dynamic-icon";
import { DeleteConfirmationDialog } from "../../hierarchy/hierarchy-components/context-menus/shared";
import { useState } from "react";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { TaskMutations } from "@/mutations/task.mutations";

interface TaskViewProps {
  taskId: string;
}

export const TaskView = observer(function TaskView({ taskId }: Readonly<TaskViewProps>) {
  const { workspaceId } = useParams({ strict: false }) as { workspaceId: string };
  const navigate = useNavigate();
  const { canCreateContent } = useWorkspaceRole();
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const taskMutations = useMemo(() => new TaskMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [isPropertiesOpen, setIsPropertiesOpen] = useState(false);
  const task = rootStore.taskStore.getById(taskId);
  const updateTask = useDebouncedTaskUpdate(taskMutations, syncEngine, taskId);

  const space = task?.spaceId ? rootStore.spaceStore.getById(task.spaceId) : undefined;
  const parentTask = task?.parentTaskId ? rootStore.taskStore.getById(task.parentTaskId) : undefined;

  const handleDelete = async () => {
    try {
      await taskMutations.delete(taskId);
      navigate({ to: "/workspaces/$workspaceId", params: { workspaceId: workspaceId || "" } });
    } catch (err) {
      console.error("Failed to delete task", err);
    }
  };

  return (
    <EntityViewFrame
      topHeader={
        <div className="flex items-center justify-between w-full">
          <Breadcrumb className="text-xs">
            <BreadcrumbList className="text-xs sm:gap-1.5">
              {space && (
                <>
                  <BreadcrumbItem>
                    <BreadcrumbLink asChild>
                      <Link
                        to="/workspaces/$workspaceId/spaces/$spaceId"
                        params={{ workspaceId, spaceId: space.id }}
                        className="flex items-center gap-1.5 text-muted-foreground hover:text-foreground transition-colors"
                      >
                        <DynamicIcon
                          name={space.icon || "Layout"}
                          size={15}
                          color={space.color || "#3b82f6"}
                          className="stroke-[2.5] shrink-0"
                        />
                        <span>{space.name}</span>
                      </Link>
                    </BreadcrumbLink>
                  </BreadcrumbItem>
                  <BreadcrumbSeparator className="[&>svg]:w-3 [&>svg]:h-3" />
                </>
              )}
              {parentTask && (
                <>
                  <BreadcrumbItem>
                    <BreadcrumbLink asChild>
                      <Link
                        to="/workspaces/$workspaceId/tasks/$taskId"
                        params={{ workspaceId, taskId: parentTask.id }}
                        className="flex items-center gap-1.5 text-muted-foreground hover:text-foreground transition-colors"
                      >
                        <DynamicIcon
                          name={parentTask.icon || "CheckSquare"}
                          size={15}
                          color={parentTask.color || "#6366f1"}
                          className="stroke-[2.5] shrink-0"
                        />
                        <span>{parentTask.name}</span>
                      </Link>
                    </BreadcrumbLink>
                  </BreadcrumbItem>
                  <BreadcrumbSeparator className="[&>svg]:w-3 [&>svg]:h-3" />
                </>
              )}
              <BreadcrumbItem>
                <BreadcrumbPage className="font-medium text-foreground flex items-center gap-1.5">
                  <DynamicIcon
                    name={task?.icon || "CheckSquare"}
                    size={15}
                    color={task?.color || "#6366f1"}
                    className="stroke-[2.5] shrink-0"
                  />
                  {task?.name ?? "Task Detail"}
                  {task && (
                    <FavoriteButton
                      entityId={task.id}
                      entityLayerType={EntityLayerType.ProjectTask}
                      iconSize={13}
                      className="opacity-100"
                    />
                  )}
                </BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
          
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-7 w-7 text-muted-foreground">
                <MoreVertical className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {canCreateContent && (
                <DropdownMenuItem
                  onClick={() => setIsDeleteOpen(true)}
                  className="text-destructive focus:text-destructive focus:bg-destructive/10 cursor-pointer"
                >
                  <Trash2 className="h-4 w-4 mr-2" />
                  Delete Task
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      }
    >
      <div className="h-full w-full flex bg-card overflow-hidden relative">
        <div className="flex-1 flex flex-col overflow-hidden relative">
          <TaskDetailCanvas taskId={taskId} />

          {/* Pinned over the content pane itself, not the app-chrome header — matches where
              Linear's issue-view expand button actually lives. Hidden while the panel is open —
              the panel's own close button takes over from there. */}
          {!isPropertiesOpen && (
            <button
              type="button"
              onClick={() => setIsPropertiesOpen(true)}
              title="Open properties panel"
              className="absolute top-3 right-3 z-10 h-7 w-7 flex items-center justify-center rounded-md border border-border/30 shadow-sm transition-colors cursor-pointer bg-card/80 backdrop-blur-sm text-muted-foreground hover:text-foreground hover:bg-muted/60"
            >
              <PanelRight className="h-3.5 w-3.5" />
            </button>
          )}
        </div>

        {isPropertiesOpen && task && (
          <div className="w-64 shrink-0 border-l border-border overflow-hidden flex flex-col">
            <div className="h-9 flex items-center justify-end px-2 border-b border-border/30 shrink-0">
              <button
                type="button"
                onClick={() => setIsPropertiesOpen(false)}
                title="Close properties panel"
                className="h-6 w-6 flex items-center justify-center rounded-md text-muted-foreground hover:text-foreground hover:bg-muted/60 transition-colors cursor-pointer"
              >
                <X className="h-3.5 w-3.5" />
              </button>
            </div>
            <TaskPropertiesPanel
              task={task}
              onStatusChange={(statusId) => updateTask({ statusId })}
              onPriorityChange={(priority: Priority) => updateTask({ priority })}
              onStartDateChange={(date) => updateTask({ startDate: date ? date.toISOString() : null })}
              onDueDateChange={(date) => updateTask({ dueDate: date ? date.toISOString() : null })}
              onClearDates={() => updateTask({ startDate: null, dueDate: null })}
            />
          </div>
        )}
      </div>

      <DeleteConfirmationDialog
        open={isDeleteOpen}
        onOpenChange={setIsDeleteOpen}
        title="Delete Task"
        description={`Are you sure you want to delete "${task?.name}"? This action cannot be undone.`}
        onConfirm={handleDelete}
      />
    </EntityViewFrame>
  );
});
