import React, { useMemo, useRef, useState } from "react";
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
import { Plus, FolderPlus, Settings, Trash2, Copy, ExternalLink, Star } from "lucide-react";
import { toast } from "sonner";
import { copyToClipboard } from "@/lib/copy-to-clipboard";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { SpaceSettingsFlow, type SpaceSettingsFlowHandle } from "@/features/workspace/contents/views/space/space-components/space-settings-flow";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { SpaceMutations } from "@/mutations/space.mutations";
import { FavoriteMutations } from "@/mutations/favorite.mutations";
import { EntityMenuContext, DeleteConfirmationDialog, EditFieldsSubmenu } from "./shared";

interface SpaceContextMenuProps {
  spaceId: string;
  spaceName: string;
  children: React.ReactNode;
  onCreateFolder?: () => void;
  onCreateTask?: () => void;
}

export const SpaceContextMenu = observer(function SpaceContextMenu({
  spaceId,
  spaceName,
  children,
  onCreateFolder,
  onCreateTask,
}: SpaceContextMenuProps) {
  const { workspaceId } = useWorkspace();
  const { canCreateContent, canDeleteSpace, isAdmin } = useWorkspaceRole();
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const spaceMutations = useMemo(() => new SpaceMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const favoriteMutations = useMemo(() => new FavoriteMutations(rootStore), [rootStore]);
  const isFavorite = rootStore.favoriteStore.isFavorite(spaceId);
  const space = rootStore.spaceStore.getById(spaceId);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const settingsFlowRef = useRef<SpaceSettingsFlowHandle>(null);

  const handleDelete = () => {
    spaceMutations.delete(spaceId).catch((err) => console.error("Failed to delete space", err));
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

        {canCreateContent && (
          <Item className="gap-2 cursor-pointer" onSelect={() => onCreateFolder?.()}>
            <FolderPlus className="h-3.5 w-3.5" />
            <span>Create Folder</span>
          </Item>
        )}

        {canCreateContent && space && (
          <EditFieldsSubmenu
            isContext={isContext}
            name={space.name}
            icon={space.icon || "LayoutGrid"}
            color={space.color || "#6366f1"}
            onRename={(name) => { if (name.trim()) spaceMutations.update(spaceId, { name: name.trim() }).catch((err) => console.error("Failed to rename space", err)); }}
            onIconColorChange={(icon, color) => spaceMutations.update(spaceId, { icon, color }).catch((err) => console.error("Failed to update space icon/color", err))}
          />
        )}

        {canCreateContent && <Separator className="bg-border/50" />}

        <Item
          className="gap-2 cursor-pointer"
          onSelect={() => favoriteMutations.toggle(spaceId, EntityLayerType.ProjectSpace).catch((err) => console.error("Failed to toggle favorite", err))}
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
            <Item className="gap-2 cursor-pointer" onSelect={() => settingsFlowRef.current?.open()}>
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

      <SpaceSettingsFlow ref={settingsFlowRef} spaceId={spaceId} />

      <DeleteConfirmationDialog
        open={isDeleteOpen}
        onOpenChange={setIsDeleteOpen}
        title="Delete Space"
        description={`Are you sure you want to delete the Space "${spaceName}"? This action cannot be undone and will delete all folders and tasks inside it.`}
        onConfirm={handleDelete}
      />
    </EntityMenuContext.Provider>
  );
});
