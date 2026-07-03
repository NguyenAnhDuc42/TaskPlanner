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
import { Plus, Trash2, Copy, ExternalLink, Star } from "lucide-react";
import { toast } from "sonner";
import { copyToClipboard } from "@/lib/copy-to-clipboard";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { CreateTaskForm } from "@/features/workspace/components/forms/create-task-form";
import { useStore } from "@/stores/root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { FolderMutations } from "@/mutations/folder.mutations";
import { FavoriteMutations } from "@/mutations/favorite.mutations";
import { EntityMenuContext, DeleteConfirmationDialog } from "./shared";

interface FolderContextMenuProps {
  folderId: string;
  folderName: string;
  children: React.ReactNode;
}

export const FolderContextMenu = observer(function FolderContextMenu({
  folderId,
  folderName,
  children,
}: FolderContextMenuProps) {
  const { workspaceId } = useWorkspace();
  const { canCreateContent, isAdmin } = useWorkspaceRole();
  const rootStore = useStore();
  const syncEngine = useSyncEngine();
  const folderMutations = useMemo(() => new FolderMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const favoriteMutations = useMemo(() => new FavoriteMutations(rootStore), [rootStore]);
  const isFavorite = rootStore.favoriteStore.isFavorite(folderId);
  const [activeForm, setActiveForm] = useState<"task" | null>(null);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);

  const handleDelete = () => {
    folderMutations.delete(folderId).catch((err) => console.error("Failed to delete folder", err));
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

        {canCreateContent && <Separator className="bg-border/50" />}

        <Item
          className="gap-2 cursor-pointer"
          onSelect={() => favoriteMutations.toggle(folderId, EntityLayerType.ProjectFolder).catch((err) => console.error("Failed to toggle favorite", err))}
        >
          <Star className={`h-3.5 w-3.5 ${isFavorite ? "fill-amber-400 text-amber-400" : ""}`} />
          <span>{isFavorite ? "Unfavorite" : "Favorite"}</span>
        </Item>

        <Separator className="bg-border/50" />

        <Item className="gap-2 cursor-pointer" onSelect={() => {
          const url = `${window.location.origin}/workspaces/${workspaceId}/folders/${folderId}`;
          copyToClipboard(url);
          toast.success("Link copied");
        }}>
          <Copy className="h-3.5 w-3.5" />
          <span>Copy Link</span>
        </Item>

        <Item className="gap-2 cursor-pointer" onSelect={() => {
          window.open(`/workspaces/${workspaceId}/folders/${folderId}`, "_blank");
        }}>
          <ExternalLink className="h-3.5 w-3.5" />
          <span>Open in New Tab</span>
        </Item>

        {isAdmin && (
          <>
            <Separator className="bg-border/50" />
            <Item variant="destructive" className="gap-2 cursor-pointer" onSelect={() => setIsDeleteOpen(true)}>
              <Trash2 className="h-3.5 w-3.5" />
              <span>Delete Folder</span>
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
          parentId={folderId}
          parentType={EntityLayerType.ProjectFolder}
          onCancel={() => setActiveForm(null)}
        />
      </DialogFormWrapper>

      <DeleteConfirmationDialog
        open={isDeleteOpen}
        onOpenChange={setIsDeleteOpen}
        title="Delete Folder"
        description={`Are you sure you want to delete the Folder "${folderName}"? This action cannot be undone and will delete all tasks inside it.`}
        onConfirm={handleDelete}
      />
    </EntityMenuContext.Provider>
  );
});
