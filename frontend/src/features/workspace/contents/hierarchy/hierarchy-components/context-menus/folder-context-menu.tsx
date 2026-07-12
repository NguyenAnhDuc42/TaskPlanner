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
import { Plus, Trash2 } from "lucide-react";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { FolderMutations } from "@/mutations/folder.mutations";
import { EntityMenuContext, DeleteConfirmationDialog, EditFieldsSubmenu } from "./shared";

interface FolderContextMenuProps {
  folderId: string;
  folderName: string;
  children: React.ReactNode;
  onCreateTask?: () => void;
}

export const FolderContextMenu = observer(function FolderContextMenu({
  folderId,
  folderName,
  children,
  onCreateTask,
}: FolderContextMenuProps) {
  const { canCreateContent, isAdmin } = useWorkspaceRole();
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const folderMutations = useMemo(() => new FolderMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const folder = rootStore.folderStore.getById(folderId);
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
          <Item className="gap-2 cursor-pointer" onSelect={() => onCreateTask?.()}>
            <Plus className="h-3.5 w-3.5" />
            <span>Create Task</span>
          </Item>
        )}

        {canCreateContent && folder && (
          <EditFieldsSubmenu
            isContext={isContext}
            name={folder.name}
            icon={folder.icon || "Folder"}
            color={folder.color || "#6366f1"}
            onRename={(name) => { if (name.trim()) folderMutations.update(folderId, { name: name.trim() }).catch((err) => console.error("Failed to rename folder", err)); }}
            onIconColorChange={(icon, color) => folderMutations.update(folderId, { icon, color }).catch((err) => console.error("Failed to update folder icon/color", err))}
          />
        )}

        {isAdmin && (
          <>
            {canCreateContent && <Separator className="bg-border/50" />}
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
