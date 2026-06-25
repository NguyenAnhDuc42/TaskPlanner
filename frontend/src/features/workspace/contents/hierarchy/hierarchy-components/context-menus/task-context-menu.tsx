import React, { useState } from "react";
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuSeparator,
  ContextMenuSub,
  ContextMenuSubContent,
  ContextMenuSubTrigger,
  ContextMenuTrigger,
} from "@/components/ui/context-menu";
import {
  DropdownMenuItem,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu";
import { Trash2, Copy, ExternalLink, Star, FolderInput, Folder } from "lucide-react";
import { toast } from "sonner";
import { copyToClipboard } from "@/lib/copy-to-clipboard";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { useDeleteTaskMutation, useBatchMoveItemsMutation } from "../../hierarchy-api";
import { useToggleFavoriteMutation } from "@/features/workspace/api";
import { useSelector, useDispatch } from "react-redux";
import { taskSelectors, folderSelectors, taskSlice } from "@/store/entityStore";
import type { AppDispatch } from "@/store";
import { EntityMenuContext, DeleteConfirmationDialog } from "./shared";
import { DynamicIcon } from "@/components/dynamic-icon";
import type { RootState } from "@/store";

interface TaskContextMenuProps {
  taskId: string;
  taskName: string;
  parentId: string;
  spaceId?: string;
  children: React.ReactNode;
}

export function TaskContextMenu({
  taskId,
  children,
  taskName,
  spaceId,
}: TaskContextMenuProps) {
  const { workspaceId } = useWorkspace();
  const { canCreateContent } = useWorkspaceRole();
  const dispatch = useDispatch<AppDispatch>();
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [deleteTask] = useDeleteTaskMutation();
  const [batchMove] = useBatchMoveItemsMutation();
  const [toggleFavorite] = useToggleFavoriteMutation();
  const isFavorite = useSelector((state: RootState) => !!taskSelectors.selectById(state, taskId)?.isFavorite);
  const task = useSelector((state: RootState) => taskSelectors.selectById(state, taskId));
  const spaceFolders = useSelector((state: RootState) =>
    spaceId ? folderSelectors.selectAll(state).filter(f => f.spaceId === spaceId) : []
  );

  const handleMoveToFolder = (targetFolderId: string | null) => {
    if (!workspaceId || !task?.spaceId) return;
    // Optimistic — update store immediately so board reflects the move at once
    dispatch(taskSlice.actions.upsert({ id: taskId, folderId: targetFolderId }));
    batchMove({
      workspaceId,
      command: {
        tasks: [{ itemId: taskId, targetSpaceId: task.spaceId, targetFolderId, newOrderKey: task.orderKey ?? "00000001" }],
      },
    });
  };

  const handleDelete = () => {
    deleteTask({ workspaceId: workspaceId || "", taskId });
    setIsDeleteOpen(false);
  };

  const renderMenuItems = (isContext: boolean) => {
    const Item = isContext ? ContextMenuItem : DropdownMenuItem;
    const Separator = isContext ? ContextMenuSeparator : DropdownMenuSeparator;

    return (
      <>
        <Item
          className="gap-2 cursor-pointer"
          onSelect={() => workspaceId && toggleFavorite({ workspaceId, entityId: taskId, entityLayerType: EntityLayerType.ProjectTask })}
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

        {canCreateContent && spaceFolders.length > 0 && isContext && (
          <>
            <Separator className="bg-border/50" />
            <ContextMenuSub>
              <ContextMenuSubTrigger className="gap-2 cursor-pointer">
                <FolderInput className="h-3.5 w-3.5" />
                <span>Move to</span>
              </ContextMenuSubTrigger>
              <ContextMenuSubContent className="w-44 bg-popover border-border/50 shadow-2xl rounded-xl p-1.5">
                {task?.folderId && (
                  <ContextMenuItem className="gap-2 cursor-pointer text-xs" onSelect={() => handleMoveToFolder(null)}>
                    <Folder className="h-3 w-3 text-muted-foreground" />
                    <span>Space level</span>
                  </ContextMenuItem>
                )}
                {spaceFolders.filter(f => f.id !== task?.folderId).map(folder => (
                  <ContextMenuItem key={folder.id} className="gap-2 cursor-pointer text-xs" onSelect={() => handleMoveToFolder(folder.id)}>
                    <DynamicIcon name={folder.icon || "Folder"} size={12} color={folder.color || "#6366f1"} />
                    <span className="truncate">{folder.name}</span>
                  </ContextMenuItem>
                ))}
              </ContextMenuSubContent>
            </ContextMenuSub>
          </>
        )}

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
}
