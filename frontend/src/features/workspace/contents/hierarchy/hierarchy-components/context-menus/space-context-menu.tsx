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
import { Plus, FolderPlus, Settings, Trash2, Copy, ExternalLink, Star } from "lucide-react";
import { toast } from "sonner";
import { copyToClipboard } from "@/lib/copy-to-clipboard";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { CreateTaskForm } from "@/features/workspace/components/forms/create-task-form";
import { CreateFolderForm } from "@/features/workspace/components/forms/create-folder-form";
import { CreateSpaceForm } from "@/features/workspace/components/forms/create-space-form";
import { useDeleteSpaceMutation } from "../../hierarchy-api";
import { useDispatch, useSelector } from "react-redux";
import { spaceSlice, spaceSelectors } from "@/store/entityStore";
import { useToggleFavoriteMutation } from "@/features/workspace/api";
import { EntityMenuContext, DeleteConfirmationDialog } from "./shared";
import type { RootState } from "@/store";

interface SpaceContextMenuProps {
  spaceId: string;
  spaceName: string;
  children: React.ReactNode;
}

export function SpaceContextMenu({
  spaceId,
  spaceName,
  children,
}: SpaceContextMenuProps) {
  const { workspaceId } = useWorkspace();
  const { canCreateContent, canDeleteSpace, isAdmin } = useWorkspaceRole();
  const [toggleFavorite] = useToggleFavoriteMutation();
  const isFavorite = useSelector((state: RootState) => !!spaceSelectors.selectById(state, spaceId)?.isFavorite);
  const dispatch = useDispatch();
  const [activeForm, setActiveForm] = useState<"task" | "folder" | "settings" | null>(null);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [deleteSpace] = useDeleteSpaceMutation();

  const handleDelete = () => {
    deleteSpace({ workspaceId: workspaceId || "", spaceId });
    setIsDeleteOpen(false);
  };

  const renderMenuItems = (isContext: boolean) => {
    const Item = isContext ? ContextMenuItem : DropdownMenuItem;
    const Separator = isContext ? ContextMenuSeparator : DropdownMenuSeparator;

    return (
      <>
        {canCreateContent && (
          <Item className="gap-2 cursor-pointer" onSelect={() => setActiveForm("task")}>
            <Plus className="h-3.5 w-3.5" />
            <span>Create Task</span>
          </Item>
        )}

        {canCreateContent && (
          <Item className="gap-2 cursor-pointer" onSelect={() => setActiveForm("folder")}>
            <FolderPlus className="h-3.5 w-3.5" />
            <span>Create Folder</span>
          </Item>
        )}

        {canCreateContent && <Separator className="bg-border/50" />}

        <Item
          className="gap-2 cursor-pointer"
          onSelect={() => workspaceId && toggleFavorite({ workspaceId, entityId: spaceId, entityLayerType: EntityLayerType.ProjectSpace })}
        >
          <Star className={`h-3.5 w-3.5 ${isFavorite ? "fill-amber-400 text-amber-400" : ""}`} />
          <span>{isFavorite ? "Unfavorite" : "Favorite"}</span>
        </Item>

        <Separator className="bg-border/50" />

        <Item className="gap-2 cursor-pointer" onSelect={() => {
          const url = `${window.location.origin}/workspaces/${workspaceId}/spaces/${spaceId}`;
          copyToClipboard(url);
          toast.success("Link copied");
        }}>
          <Copy className="h-3.5 w-3.5" />
          <span>Copy Link</span>
        </Item>

        <Item className="gap-2 cursor-pointer" onSelect={() => {
          window.open(`/workspaces/${workspaceId}/spaces/${spaceId}`, "_blank");
        }}>
          <ExternalLink className="h-3.5 w-3.5" />
          <span>Open in New Tab</span>
        </Item>

        {isAdmin && (
          <>
            <Separator className="bg-border/50" />
            <Item className="gap-2 cursor-pointer" onSelect={() => setActiveForm("settings")}>
              <Settings className="h-3.5 w-3.5" />
              <span>Space Settings</span>
            </Item>
          </>
        )}

        {canDeleteSpace && (
          <>
            {!isAdmin && <Separator className="bg-border/50" />}
            <Item variant="destructive" className="gap-2 cursor-pointer" onSelect={() => setIsDeleteOpen(true)}>
              <Trash2 className="h-3.5 w-3.5" />
              <span>Delete Space</span>
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

      <DialogFormWrapper
        open={activeForm === "task"}
        onOpenChange={(open) => !open && setActiveForm(null)}
        title="Create New Task"
        trigger={null}
      >
        <CreateTaskForm 
          parentId={spaceId}
          parentType={EntityLayerType.ProjectSpace}
          onSuccess={() => {
            setActiveForm(null);
            // Direct relational database state sync in Redux
            dispatch(spaceSlice.actions.upsert({ id: spaceId, hasTasks: true }));
          }}
          onCancel={() => setActiveForm(null)}
        />
      </DialogFormWrapper>

      <DialogFormWrapper
        open={activeForm === "folder"}
        onOpenChange={(open) => !open && setActiveForm(null)}
        title="Create New Folder"
        trigger={null}
      >
        <CreateFolderForm 
          spaceId={spaceId}
          onSuccess={() => {
            setActiveForm(null);
            dispatch(spaceSlice.actions.upsert({ id: spaceId, hasFolders: true }));
          }}
          onCancel={() => setActiveForm(null)}
        />
      </DialogFormWrapper>

      <DialogFormWrapper
        open={activeForm === "settings"}
        onOpenChange={(open) => !open && setActiveForm(null)}
        title="Space Settings"
        trigger={null}
        contentClassName="max-w-3xl p-0 overflow-hidden border-none shadow-2xl rounded-2xl bg-background outline-none ring-1 ring-border/50"
      >
        <CreateSpaceForm 
          onSuccess={() => setActiveForm(null)}
          onCancel={() => setActiveForm(null)}
        />
      </DialogFormWrapper>

      <DeleteConfirmationDialog
        open={isDeleteOpen}
        onOpenChange={setIsDeleteOpen}
        title="Delete Space"
        description={`Are you sure you want to delete the Space "${spaceName}"? This action cannot be undone and will delete all folders and tasks inside it.`}
        onConfirm={handleDelete}
      />
    </EntityMenuContext.Provider>
  );
}
