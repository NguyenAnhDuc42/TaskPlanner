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
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { CreateTaskForm } from "@/features/workspace/components/forms/create-task-form";
import { useDeleteFolder } from "../../hierarchy-api";
import { workspaceKeys } from "@/features/main/query-keys";
import { useQueryClient } from "@tanstack/react-query";
import { useHierarchyStore } from "../../use-hierarchy-store";
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
  const queryClient = useQueryClient();
  const [activeForm, setActiveForm] = useState<"task" | null>(null);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);

  const { mutate: deleteFolder } = useDeleteFolder(workspaceId || "");

  const handleDelete = () => {
    deleteFolder(folderId);
    setIsDeleteOpen(false);
  };

  const renderMenuItems = (isContext: boolean) => {
    const Item = isContext ? ContextMenuItem : DropdownMenuItem;
    const Separator = isContext ? ContextMenuSeparator : DropdownMenuSeparator;

    return (
      <>
        <Item className="gap-2 cursor-pointer" onSelect={() => setActiveForm("task")}>
          <Plus className="h-3.5 w-3.5" />
          <span>Create Task</span>
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

        <Item variant="destructive" className="gap-2 cursor-pointer" onSelect={() => setIsDeleteOpen(true)}>
          <Trash2 className="h-3.5 w-3.5" />
          <span>Delete Folder</span>
        </Item>
      </>
    );
  };

  return (
    <EntityMenuContext.Provider value={{ renderMenuItems }}>
      <ContextMenu>
        <ContextMenuTrigger asChild>
          {children}
        </ContextMenuTrigger>
        <ContextMenuContent className="w-52 bg-background/95 backdrop-blur-md border-border/50 shadow-2xl rounded-xl p-1.5 animate-in fade-in-0 zoom-in-95">
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
            queryClient.invalidateQueries({
              queryKey: [...workspaceKeys.all, "folder", folderId, "items"],
            });
            
            // Update hasTasks flag in hierarchy store (for folders)
            const store = useHierarchyStore.getState();
            if (store.folders[folderId]) {
              useHierarchyStore.setState({
                folders: {
                  ...store.folders,
                  [folderId]: { ...store.folders[folderId], hasTasks: true }
                }
              });
            }
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
