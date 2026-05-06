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
  MoreHorizontal
} from "lucide-react";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { CreateTaskForm } from "@/features/workspace/components/forms/create-task-form";
import { CreateFolderForm } from "@/features/workspace/components/forms/create-folder-form";
import { CreateSpaceForm } from "@/features/workspace/components/forms/create-space-form";

interface EntityMenuContextType {
  renderMenuItems: (isContext: boolean) => React.ReactNode;
}

const EntityMenuContext = createContext<EntityMenuContextType | null>(null);

interface EntityContextMenuProps {
  entityId: string;
  entityType: EntityLayerType;
  entityName: string;
  spaceId?: string;
  children: React.ReactNode;
}

export function EntityContextMenu({
  entityId,
  entityType,
  entityName,
  spaceId,
  children,
}: EntityContextMenuProps) {
  const { workspaceId } = useWorkspace();
  const [activeForm, setActiveForm] = useState<"task" | "folder" | "space" | "settings" | null>(null);

  const renderMenuItems = (isContext: boolean) => {
    const Item = isContext ? ContextMenuItem : DropdownMenuItem;
    const Separator = isContext ? ContextMenuSeparator : DropdownMenuSeparator;

    return (
      <>
        {(entityType === EntityLayerType.ProjectSpace || entityType === EntityLayerType.ProjectFolder) && (
          <Item 
            className="gap-2" 
            onSelect={() => {
              // We don't preventDefault here so the menu closes, 
              // which avoids focus conflicts with the Dialog.
              setActiveForm("task");
            }}
          >
            <Plus className="h-3.5 w-3.5" />
            <span>Create Task</span>
          </Item>
        )}

        {entityType === EntityLayerType.ProjectSpace && (
          <Item 
            className="gap-2" 
            onSelect={() => setActiveForm("folder")}
          >
            <FolderPlus className="h-3.5 w-3.5" />
            <span>Create Folder</span>
          </Item>
        )}

        <Separator className="bg-border/50" />

        <Item className="gap-2">
          <Copy className="h-3.5 w-3.5" />
          <span>Copy Link</span>
        </Item>
        
        <Item className="gap-2">
          <ExternalLink className="h-3.5 w-3.5" />
          <span>Open in New Tab</span>
        </Item>

        <Separator className="bg-border/50" />

        {entityType === EntityLayerType.ProjectSpace && (
          <Item 
            className="gap-2" 
            onSelect={() => setActiveForm("settings")}
          >
            <Settings className="h-3.5 w-3.5" />
            <span>Space Settings</span>
          </Item>
        )}

        <Item variant="destructive" className="gap-2">
          <Trash2 className="h-3.5 w-3.5" />
          <span>Delete {entityType === EntityLayerType.ProjectSpace ? "Space" : entityType === EntityLayerType.ProjectFolder ? "Folder" : "Task"}</span>
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
          parentId={entityId}
          parentType={entityType}
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
          spaceId={entityId}
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
