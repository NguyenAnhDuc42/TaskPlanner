
import { List, MoreHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import type { ListNode as ListNodeType } from "@/features/workspace/workspacetype";
import Link from "next/link";
import { useWorkspaceStore } from "@/utils/workspace-store";
import { CreateTaskButton } from "@/components/sidebar/forms/create-task-form";
import React from "react";
import { cn } from "@/lib/utils";

export type ListContext = {
  spaceId: string;
  folderId?: string;
  listId: string;
};

interface ListNodeProps {
  list: ListNodeType;
  context: ListContext;
  isUnderFolder?: boolean;
  onClick?: () => void;
}

export function ListNode({ list, context, isUnderFolder = false, onClick }: ListNodeProps) {
  const { selectedWorkspaceId } = useWorkspaceStore();
  const [showCreateTaskModal, setShowCreateTaskModal] = React.useState(false);

  const listUrl = selectedWorkspaceId
    ? `/ws/${selectedWorkspaceId}/l/${context.listId}`
    : "#";

  return (
    <div className="relative">
      {/* Horizontal connecting line to parent */}
      <div className={cn(`absolute top-3.5 w-3 h-px bg-sidebar-border/30`, isUnderFolder ? 'left-[-0.5rem]' : 'left-[-0.5rem]')} />
      
      <div className="flex items-center group/list hover:bg-sidebar-accent/50 rounded">
        <Button
          variant="ghost"
          className="flex-1 justify-start h-7 px-2 py-1 text-xs hover:bg-transparent group-hover/list:bg-transparent rounded focus-visible:ring-0 focus-visible:ring-offset-0"
          onClick={onClick}
        >
          <Link href={listUrl} className="flex items-center gap-2 w-full" passHref>
            <List className="size-3 text-sidebar-foreground/60" />
            <span className="truncate text-sidebar-foreground/80">{list.name}</span>
          </Link>
        </Button>
        
        {/* Actions */}
        <div className="flex items-center opacity-0 group-hover/list:opacity-100">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="size-5 hover:bg-sidebar-accent rounded focus-visible:ring-0 focus-visible:ring-offset-0"
              >
                <MoreHorizontal className="size-3" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start" className="w-48">
              <DropdownMenuItem>Rename list</DropdownMenuItem>
              <DropdownMenuItem>List settings</DropdownMenuItem>
              <DropdownMenuItem>Duplicate list</DropdownMenuItem>
              <DropdownMenuItem className="text-destructive">Delete list</DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
          
          <CreateTaskButton
            listContext={context}
            listName={list.name}
            isOpen={showCreateTaskModal}
            onOpenChange={setShowCreateTaskModal}
          />
        </div>
      </div>
    </div>
  );
}
  