import { EntityViewFrame } from "../entity-view-frame";
import { TaskDetailCanvas } from "./components/task-detail-canvas";
import { useNavigate, useParams } from "@tanstack/react-router";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import { MoreVertical, Trash2 } from "lucide-react";
import { FavoriteButton } from "@/components/favorite-button";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useDeleteTaskMutation } from "../../hierarchy/hierarchy-api";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { Link } from "@tanstack/react-router";
import { useSpaceDetail } from "../space/space-api";
import { useFolderDetail } from "../folder/folder-api";
import { useSelector } from "react-redux";
import { taskSelectors } from "@/store/entityStore";
import type { RootState } from "@/store";
import { DynamicIcon } from "@/components/dynamic-icon";
import { DeleteConfirmationDialog } from "../../hierarchy/hierarchy-components/context-menus/shared";
import { useState } from "react";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";

interface TaskViewProps {
  taskId: string;
}

export function TaskView({ taskId }: Readonly<TaskViewProps>) {
  const { workspaceId } = useParams({ strict: false }) as { workspaceId: string };
  const navigate = useNavigate();
  const { canCreateContent } = useWorkspaceRole();
  const [deleteTask] = useDeleteTaskMutation();
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const task = useSelector((state: RootState) => taskSelectors.selectById(state, taskId));

  const space = useSpaceDetail(task?.spaceId ?? "");
  const folder = useFolderDetail(task?.folderId ?? "");
  const parentTask = useSelector((state: RootState) => task?.parentTaskId ? taskSelectors.selectById(state, task.parentTaskId) : undefined);

  const handleDelete = async () => {
    try {
      await deleteTask({ workspaceId: workspaceId || "", taskId }).unwrap();
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
              {folder && (
                <>
                  <BreadcrumbItem>
                    <BreadcrumbLink asChild>
                      <Link
                        to="/workspaces/$workspaceId/folders/$folderId"
                        params={{ workspaceId, folderId: folder.id }}
                        className="flex items-center gap-1.5 text-muted-foreground hover:text-foreground transition-colors"
                      >
                        <DynamicIcon
                          name={folder.icon || "Folder"}
                          size={15}
                          color={folder.color || "#6366f1"}
                          className="stroke-[2.5] shrink-0"
                        />
                        <span>{folder.name}</span>
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
      <div className="h-full w-full flex flex-col bg-card overflow-hidden relative">
        <TaskDetailCanvas taskId={taskId} />
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
}
