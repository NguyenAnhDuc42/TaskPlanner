import { createContext, useContext, useMemo, useState } from "react";
import { observer } from "mobx-react-lite";
import { Link, Outlet, useNavigate, useParams } from "@tanstack/react-router";
import { EntityViewFrame } from "./entity-view-frame";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import { MoreVertical, Trash2 } from "lucide-react";
import { FavoriteButton } from "@/components/favorite-button";
import { DynamicIcon } from "@/components/dynamic-icon";
import { EntityLayerType } from "@/types/entity-layer-type";
import { DeleteConfirmationDialog } from "../hierarchy/hierarchy-components/context-menus/shared";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { useDocumentOutline } from "@/features/workspace/context/document-editor-context";
import { DocumentOutlineRail } from "@/components/document-outline-rail";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { TaskMutations } from "@/mutations/task.mutations";
import { SpaceMutations } from "@/mutations/space.mutations";

const EntityShellUiContext = createContext<{ setRightPanelOpen: (open: boolean) => void } | null>(null);

export function useEntityShellUi() {
  return useContext(EntityShellUiContext);
}

export const EntityShell = observer(function EntityShell() {
  const { workspaceId, taskId, spaceId } = useParams({ strict: false }) as {
    workspaceId: string;
    taskId?: string;
    spaceId?: string;
  };

  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const navigate = useNavigate();
  const { canCreateContent, isAdmin } = useWorkspaceRole();

  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [rightPanelOpen, setRightPanelOpen] = useState(false);
  const uiValue = useMemo(() => ({ setRightPanelOpen }), []);

  const entityType: "task" | "space" = taskId ? "task" : "space";
  const task = taskId ? rootStore.taskStore.getById(taskId) : undefined;
  const space = spaceId ? rootStore.spaceStore.getById(spaceId) : undefined;
  const taskSpace = task?.spaceId ? rootStore.spaceStore.getById(task.spaceId) : undefined;
  const parentTask = task?.parentTaskId ? rootStore.taskStore.getById(task.parentTaskId) : undefined;

  const documentId = entityType === "task" ? task?.defaultDocumentId : space?.defaultDocumentId;
  const { outline, navigate: navigateToHeading } = useDocumentOutline(documentId);

  const taskMutations = useMemo(() => new TaskMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const spaceMutations = useMemo(() => new SpaceMutations(rootStore, syncEngine), [rootStore, syncEngine]);

  const canDelete = entityType === "task" ? canCreateContent : isAdmin;
  const entityName = entityType === "task" ? task?.name : space?.name;

  const handleDelete = async () => {
    try {
      if (entityType === "task" && taskId) {
        await taskMutations.delete(taskId);
      } else if (spaceId) {
        await spaceMutations.delete(spaceId);
      }
      navigate({ to: "/workspaces/$workspaceId", params: { workspaceId: workspaceId || "" } });
    } catch (err) {
      console.error(`Failed to delete ${entityType}`, err);
    }
  };

  const taskHeader = (
    <Breadcrumb className="text-xs">
      <BreadcrumbList className="text-xs sm:gap-1.5">
        {taskSpace && (
          <>
            <BreadcrumbItem>
              <BreadcrumbLink asChild>
                <Link
                  to="/workspaces/$workspaceId/spaces/$spaceId"
                  params={{ workspaceId, spaceId: taskSpace.id }}
                  className="flex items-center gap-1.5 text-muted-foreground hover:text-foreground transition-colors"
                >
                  <DynamicIcon
                    name={taskSpace.icon || "Layout"}
                    size={15}
                    color={taskSpace.color || "#3b82f6"}
                    className="stroke-[2.5] shrink-0"
                  />
                  <span>{taskSpace.name}</span>
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
  );

  const spaceHeader = (
    <div className="flex items-center gap-1.5 text-xs min-w-0">
      <DynamicIcon
        name={space?.icon ?? "LayoutGrid"}
        size={14}
        color={space?.color ?? "#3b82f6"}
        className="shrink-0"
      />
      <span className="font-semibold text-foreground/80 truncate">
        {space?.name ?? "Space"}
      </span>
      {space && spaceId && (
        <FavoriteButton
          entityId={spaceId}
          entityLayerType={EntityLayerType.ProjectSpace}
          iconSize={13}
          className="opacity-100 shrink-0"
        />
      )}
    </div>
  );

  return (
    <EntityViewFrame
      topHeader={
        <div className="flex items-center justify-between w-full">
          {entityType === "task" ? taskHeader : spaceHeader}

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-7 w-7 text-muted-foreground">
                <MoreVertical className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {canDelete && (
                <DropdownMenuItem
                  onClick={() => setIsDeleteOpen(true)}
                  className="text-destructive focus:text-destructive focus:bg-destructive/10 cursor-pointer"
                >
                  <Trash2 className="h-4 w-4 mr-2" />
                  Delete {entityType === "task" ? "Task" : "Space"}
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      }
    >
      <EntityShellUiContext.Provider value={uiValue}>
        <div className="h-full w-full relative overflow-hidden">
          <Outlet />

          {!rightPanelOpen && (
            <div className="absolute inset-y-3 right-4 z-10 w-7 pointer-events-none">
              <div className="pointer-events-auto absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2">
                <DocumentOutlineRail outline={outline} onNavigate={navigateToHeading} />
              </div>
            </div>
          )}
        </div>
      </EntityShellUiContext.Provider>

      <DeleteConfirmationDialog
        open={isDeleteOpen}
        onOpenChange={setIsDeleteOpen}
        title={entityType === "task" ? "Delete Task" : "Delete Space"}
        description={
          entityType === "task"
            ? `Are you sure you want to delete "${entityName}"? This action cannot be undone.`
            : `Are you sure you want to delete "${entityName}"? This will delete all folders and tasks inside it and cannot be undone.`
        }
        onConfirm={handleDelete}
      />
    </EntityViewFrame>
  );
});
