import React, { useMemo, useState } from "react";
import { observer } from "mobx-react-lite";
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuSeparator,
  ContextMenuTrigger,
} from "@/components/ui/context-menu";
import {
  DropdownMenuItem,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu";
import { Trash2, Copy, ExternalLink, Star } from "lucide-react";
import { toast } from "sonner";
import { copyToClipboard } from "@/lib/copy-to-clipboard";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { useDebouncedFlush } from "@/sync/use-debounced-flush";
import { TaskMutations } from "@/mutations/task.mutations";
import { FavoriteMutations } from "@/mutations/favorite.mutations";
import { EntityMenuContext, DeleteConfirmationDialog, EditFieldsSubmenu } from "./shared";

interface TaskContextMenuProps {
  taskId: string;
  taskName: string;
  parentId: string;
  children: React.ReactNode;
}

export const TaskContextMenu = observer(function TaskContextMenu({
  taskId,
  children,
  taskName,
}: TaskContextMenuProps) {
  const { workspaceId } = useWorkspace();
  const { canCreateContent } = useWorkspaceRole();
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const taskMutations = useMemo(() => new TaskMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const favoriteMutations = useMemo(() => new FavoriteMutations(rootStore), [rootStore]);
  const { scheduleFlush } = useDebouncedFlush(syncEngine);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const isFavorite = rootStore.favoriteStore.isFavorite(taskId);
  const task = rootStore.taskStore.getById(taskId);

  const handleDelete = () => {
    taskMutations.delete(taskId).catch((err) => console.error("Failed to delete task", err));
    setIsDeleteOpen(false);
  };

  const renderMenuItems = (isContext: boolean) => {
    const Item = isContext ? ContextMenuItem : DropdownMenuItem;
    const Separator = isContext ? ContextMenuSeparator : DropdownMenuSeparator;

    return (
      <>
        {canCreateContent && task && (
          <EditFieldsSubmenu
            isContext={isContext}
            name={task.name}
            icon={task.icon || "CheckSquare"}
            color={task.color || "#6366f1"}
            onRename={(name) => {
              if (!name.trim()) return;
              taskMutations.updateLocal(taskId, { name: name.trim() }).catch((err) => console.error("Failed to rename task", err));
              scheduleFlush();
            }}
            onIconColorChange={(icon, color) => {
              taskMutations.updateLocal(taskId, { icon, color }).catch((err) => console.error("Failed to update task icon/color", err));
              scheduleFlush();
            }}
          />
        )}

        {canCreateContent && <Separator className="bg-border/50" />}

        <Item
          className="gap-2 cursor-pointer"
          onSelect={() => favoriteMutations.toggle(taskId, EntityLayerType.ProjectTask).catch((err) => console.error("Failed to toggle favorite", err))}
        >
          <Star className={`h-3.5 w-3.5 ${isFavorite ? "fill-amber-400 text-amber-400" : ""}`} />
          <span>{isFavorite ? "Unfavorite" : "Favorite"}</span>
        </Item>

        <Separator className="bg-border/50" />

        <Item className="gap-2 cursor-pointer" onSelect={() => {
          const url = `${window.location.origin}/workspaces/${workspaceId}/tasks/${taskId}`;
          copyToClipboard(url);
          toast.success("Link copied");
        }}>
          <Copy className="h-3.5 w-3.5" />
          <span>Copy Link</span>
        </Item>

        <Item className="gap-2 cursor-pointer" onSelect={() => {
          window.open(`/workspaces/${workspaceId}/tasks/${taskId}`, "_blank");
        }}>
          <ExternalLink className="h-3.5 w-3.5" />
          <span>Open in New Tab</span>
        </Item>

        {canCreateContent && (
          <>
            <Separator className="bg-border/50" />
            <Item variant="destructive" className="gap-2 cursor-pointer" onSelect={() => setIsDeleteOpen(true)}>
              <Trash2 className="h-3.5 w-3.5" />
              <span>Delete Task</span>
            </Item>
          </>
        )}
      </>
    );
  };

  return (
    <EntityMenuContext.Provider value={{ renderMenuItems }}>
      <ContextMenu>
        <ContextMenuTrigger asChild>
          {children}
        </ContextMenuTrigger>
        <ContextMenuContent
          onCloseAutoFocus={(e) => e.preventDefault()}
          className="w-52 bg-popover text-popover-foreground border-border/50 shadow-2xl rounded-xl p-1.5 animate-in fade-in-0 zoom-in-95"
        >
          {renderMenuItems(true)}
        </ContextMenuContent>
      </ContextMenu>

      <DeleteConfirmationDialog
        open={isDeleteOpen}
        onOpenChange={setIsDeleteOpen}
        title="Delete Task"
        description={`Are you sure you want to delete the Task "${taskName}"? This action cannot be undone.`}
        onConfirm={handleDelete}
      />
    </EntityMenuContext.Provider>
  );
});
