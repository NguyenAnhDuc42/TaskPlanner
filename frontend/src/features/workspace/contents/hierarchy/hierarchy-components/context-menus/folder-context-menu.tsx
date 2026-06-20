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
  Trash2, 
  Copy, 
  ExternalLink,
} from "lucide-react";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { CreateTaskForm } from "@/features/workspace/components/forms/create-task-form";
import { useDeleteFolderMutation } from "../../hierarchy-api";
import { useDispatch } from "react-redux";
import { folderSlice } from "@/store/entityStore";
import { EntityMenuContext, DeleteConfirmationDialog } from "./shared";

interface FolderContextMenuProps {
  folderId: string;
  folderName: string;
  children: React.ReactNode;
}

export function FolderContextMenu({
  folderId,
  folderName,
  children,
}: FolderContextMenuProps) {
  const { workspaceId } = useWorkspace();
  const { canCreateContent, isAdmin } = useWorkspaceRole();
  const dispatch = useDispatch();
  const [activeForm, setActiveForm] = useState<"task" | null>(null);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [deleteFolder] = useDeleteFolderMutation();

  const handleDelete = () => {
    deleteFolder({ workspaceId: workspaceId || "", folderId });
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

        <Item className="gap-2 cursor-pointer">
          <Copy className="h-3.5 w-3.5" />
          <span>Copy Link</span>
        </Item>

        <Item className="gap-2 cursor-pointer">
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
          parentId={folderId}
          parentType={EntityLayerType.ProjectFolder}
          onSuccess={() => {
            setActiveForm(null);
            dispatch(folderSlice.actions.upsert({ id: folderId, hasTasks: true }));
          }}
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
}
