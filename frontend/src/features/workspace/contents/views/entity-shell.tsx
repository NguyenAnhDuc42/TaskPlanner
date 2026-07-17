import { createContext, useContext, useMemo, useState } from "react";
import { observer } from "mobx-react-lite";
import { Link, Outlet, useNavigate, useParams } from "@tanstack/react-router";
import { ChevronRight } from "lucide-react";
import { EntityViewFrame } from "./entity-view-frame";
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
  const { canCreateContent } = useWorkspaceRole();

  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [rightPanelOpen, setRightPanelOpen] = useState(false);
  const uiValue = useMemo(() => ({ setRightPanelOpen }), []);

  const entityType: "task" | "space" = taskId ? "task" : "space";
  const task = taskId ? rootStore.taskStore.getById(taskId) : undefined;
  const space = spaceId ? rootStore.spaceStore.getById(spaceId) : undefined;
  const taskSpace = task?.spaceId ? rootStore.spaceStore.getById(task.spaceId) : undefined;

  const documentId = entityType === "task" ? task?.defaultDocumentId : space?.defaultDocumentId;
  const { outline, navigate: navigateToHeading } = useDocumentOutline(documentId);

  const taskMutations = useMemo(() => new TaskMutations(rootStore, syncEngine), [rootStore, syncEngine]);

  const handleDelete = async () => {
    if (!taskId) return;
    try {
      await taskMutations.delete(taskId);
      navigate({ to: "/workspaces/$workspaceId", params: { workspaceId: workspaceId || "" } });
    } catch (err) {
      console.error("Failed to delete task", err);
    }
  };

  const taskHeader = (
    <div className="flex items-center justify-between w-full">
      <div className="flex items-center gap-1.5 text-xs min-w-0">
        {taskSpace && (
          <>
            <Link
              to="/workspaces/$workspaceId/spaces/$spaceId"
              params={{ workspaceId, spaceId: taskSpace.id }}
              className="flex items-center gap-1.5 min-w-0 text-muted-foreground hover:text-foreground transition-colors"
            >
              <DynamicIcon
                name={taskSpace.icon || "Orbit"}
                size={14}
                color={taskSpace.color || "#ffffff"}
                className="shrink-0"
              />
              <span className="truncate">{taskSpace.name}</span>
            </Link>
            <ChevronRight className="h-3 w-3 text-muted-foreground/50 shrink-0" />
          </>
        )}
        <DynamicIcon
          name={task?.icon || "Circle"}
          size={14}
          color={task?.color || "#ffffff"}
          className="shrink-0"
        />
        <span className="font-semibold text-foreground/80 truncate">
          {task?.name ?? "Task"}
        </span>
        {task && taskId && (
          <FavoriteButton
            entityId={taskId}
            entityLayerType={EntityLayerType.ProjectTask}
            iconSize={13}
            className="opacity-100 shrink-0"
          />
        )}
      </div>

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
  );

  return (
    <EntityViewFrame topHeader={entityType === "task" ? taskHeader : undefined}>
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
        title="Delete Task"
        description={`Are you sure you want to delete "${task?.name}"? This action cannot be undone.`}
        onConfirm={handleDelete}
      />
    </EntityViewFrame>
  );
});
