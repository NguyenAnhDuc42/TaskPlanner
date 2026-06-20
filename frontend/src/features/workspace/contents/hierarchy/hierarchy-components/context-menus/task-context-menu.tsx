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
} from "lucide-react";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { useDeleteTaskMutation } from "../../hierarchy-api";
import { EntityMenuContext, DeleteConfirmationDialog } from "./shared";

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
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [deleteTask] = useDeleteTaskMutation();

  const handleDelete = () => {
    deleteTask({ workspaceId: workspaceId || "", taskId });
    setIsDeleteOpen(false);
  };

  const renderMenuItems = React.useCallback((isContext: boolean) => {
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
  }, [taskId, workspaceId]);

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
