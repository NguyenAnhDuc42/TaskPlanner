import React, { useState, createContext, useContext } from "react";
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuSeparator,
  ContextMenuTrigger,
} from "@/components/ui/context-menu";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
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

import { useDeleteSpace, useDeleteFolder, useDeleteTask } from "../hierarchy-api";
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from "@/components/ui/alert-dialog";

interface EntityMenuContextType {
  renderMenuItems: (isContext: boolean) => React.ReactNode;
}

const EntityMenuContext = createContext<EntityMenuContextType | null>(null);

interface DeleteConfirmationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description: string;
  onConfirm: () => void;
}

function DeleteConfirmationDialog({
  open,
  onOpenChange,
  title,
  description,
  onConfirm,
}: DeleteConfirmationDialogProps) {
  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent size="sm">
        <AlertDialogHeader>
          <AlertDialogTitle className="text-sm font-bold">{title}</AlertDialogTitle>
          <AlertDialogDescription className="text-xs text-muted-foreground">
            {description}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter className="mt-2">
          <AlertDialogCancel size="sm" className="text-xs cursor-pointer">Cancel</AlertDialogCancel>
          <AlertDialogAction 
            size="sm" 
            variant="destructive" 
            className="text-xs cursor-pointer"
            onClick={(e) => {
              e.stopPropagation();
              onConfirm();
            }}
          >
            Delete
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

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
  const [activeForm, setActiveForm] = useState<"task" | "folder" | "settings" | null>(null);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);

  const { mutate: deleteSpace } = useDeleteSpace(workspaceId || "");

  const handleDelete = () => {
    deleteSpace(spaceId);
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
          parentId={spaceId}
          parentType={EntityLayerType.ProjectSpace}
          onSuccess={() => setActiveForm(null)}
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
          onSuccess={() => setActiveForm(null)}
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

interface FolderContextMenuProps {
  folderId: string;
  folderName: string;
  spaceId: string;
  children: React.ReactNode;
}

export function FolderContextMenu({
  folderId,
  folderName,
  children,
}: FolderContextMenuProps) {
  const { workspaceId } = useWorkspace();
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
          onSuccess={() => setActiveForm(null)}
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

interface TaskContextMenuProps {
  taskId: string;
  taskName: string;
  parentId: string;
  children: React.ReactNode;
}

export function TaskContextMenu({
  taskId,
  taskName,
  parentId,
  children,
}: TaskContextMenuProps) {
  const { workspaceId } = useWorkspace();
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const { mutate: deleteTask } = useDeleteTask(workspaceId || "", parentId);

  const handleDelete = () => {
    deleteTask(taskId);
    setIsDeleteOpen(false);
  };

  const renderMenuItems = (isContext: boolean) => {
    const Item = isContext ? ContextMenuItem : DropdownMenuItem;
    const Separator = isContext ? ContextMenuSeparator : DropdownMenuSeparator;

    return (
      <>
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
          <span>Delete Task</span>
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

export function EntityMenuTrigger({ children }: { children: React.ReactNode }) {
  const context = useContext(EntityMenuContext);
  if (!context) throw new Error("EntityMenuTrigger must be used within EntityContextMenu");

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        {children}
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" side="right" className="w-52 bg-background/95 backdrop-blur-md border-border/50 shadow-2xl rounded-xl p-1.5 animate-in fade-in-0 zoom-in-95">
        {context.renderMenuItems(false)}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
