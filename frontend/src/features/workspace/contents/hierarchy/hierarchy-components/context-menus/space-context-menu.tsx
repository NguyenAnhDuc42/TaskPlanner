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
  Plus, 
  FolderPlus, 
  Settings, 
  Trash2, 
  Copy, 
  ExternalLink,
} from "lucide-react";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { CreateTaskForm } from "@/features/workspace/components/forms/create-task-form";
import { CreateFolderForm } from "@/features/workspace/components/forms/create-folder-form";
import { CreateSpaceForm } from "@/features/workspace/components/forms/create-space-form";
import { useDeleteSpaceMutation } from "../../hierarchy-api";
import { useDispatch } from "react-redux";
import { spaceSlice } from "@/store/entityStore";
import { EntityMenuContext, DeleteConfirmationDialog } from "./shared";

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
  const dispatch = useDispatch();
  const [activeForm, setActiveForm] = useState<"task" | "folder" | "settings" | null>(null);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [deleteSpace] = useDeleteSpaceMutation();
  

  const handleDelete = () => {
    deleteSpace({ workspaceId: workspaceId || "", spaceId });
    setIsDeleteOpen(false);
  };

  const renderMenuItems = React.useCallback((isContext: boolean) => {
    const Item = isContext ? ContextMenuItem : DropdownMenuItem;
    const Separator = isContext ? ContextMenuSeparator : DropdownMenuSeparator;

    return (
      <>
        <Item className="gap-2 cursor-pointer" onSelect={() => setActiveForm("task")}>
          <Plus className="h-3.5 w-3.5" />
          <span>Create Task</span>
        </Item>

        <Item className="gap-2 cursor-pointer" onSelect={() => setActiveForm("folder")}>
          <FolderPlus className="h-3.5 w-3.5" />
          <span>Create Folder</span>
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

        <Separator className="bg-border/50" />

        <Item className="gap-2 cursor-pointer" onSelect={() => setActiveForm("settings")}>
          <Settings className="h-3.5 w-3.5" />
          <span>Space Settings</span>
        </Item>

        <Item variant="destructive" className="gap-2 cursor-pointer" onSelect={() => setIsDeleteOpen(true)}>
          <Trash2 className="h-3.5 w-3.5" />
          <span>Delete Space</span>
        </Item>
      </>
    );
  }, [spaceId, workspaceId]);

  const contextValue = React.useMemo(() => ({ renderMenuItems }), [renderMenuItems]);
  return (
    <EntityMenuContext.Provider value={contextValue}>
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
