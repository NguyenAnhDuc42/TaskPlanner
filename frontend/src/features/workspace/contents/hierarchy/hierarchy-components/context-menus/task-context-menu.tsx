import React, { useState } from "react";
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
import {
  Trash2,
  Copy,
  ExternalLink,
  Star,
} from "lucide-react";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { useDeleteTaskMutation } from "../../hierarchy-api";
import { useToggleFavoriteMutation } from "@/features/workspace/api";
import { useSelector } from "react-redux";
import { taskSelectors } from "@/store/entityStore";
import { EntityMenuContext, DeleteConfirmationDialog } from "./shared";
import type { RootState } from "@/store";

interface TaskContextMenuProps {
  taskId: string;
  taskName: string;
  parentId: string;
  children: React.ReactNode;
}

export function TaskContextMenu({
  taskId,
  children,
  taskName,
}: TaskContextMenuProps) {
  const { workspaceId } = useWorkspace();
  const { canCreateContent } = useWorkspaceRole();
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [deleteTask] = useDeleteTaskMutation();
  const [toggleFavorite] = useToggleFavoriteMutation();
  const isFavorite = useSelector((state: RootState) => !!taskSelectors.selectById(state, taskId)?.isFavorite);

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

        <Item className="gap-2 cursor-pointer">
          <Copy className="h-3.5 w-3.5" />
          <span>Copy Link</span>
        </Item>
        
        <Item className="gap-2 cursor-pointer">
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
          className="w-52 bg-background/95 backdrop-blur-md border-border/50 shadow-2xl rounded-xl p-1.5 animate-in fade-in-0 zoom-in-95"
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
