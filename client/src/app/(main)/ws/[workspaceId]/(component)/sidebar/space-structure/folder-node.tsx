import { useState } from "react";
import { Folder, MoreHorizontal, Plus, ChevronRight, ChevronDown } from "lucide-react";

import type { FolderNode as FolderNodeType } from "@/features/workspace/workspacetype";
import { ListNode } from "./list-node";
import React from "react";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import { Button } from "@/components/ui/button";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";

export type FolderContext = {
  spaceId: string;
  folderId: string;
};

interface FolderNodeProps {
  folder: FolderNodeType;
  context: Omit<FolderContext, "folderId">;
  onClick?: () => void;
}

export function FolderNode({ folder, context, onClick }: FolderNodeProps) {
  const [isOpen, setIsOpen] = useState(false);
  const hasChildren = folder.lists.length > 0;
  const folderContext: FolderContext = { ...context, folderId: folder.id };
  
  return (
    <div className="space-y-0">
      <Collapsible open={isOpen} onOpenChange={setIsOpen}>
        <div className="relative">
          
          <div className="flex items-center group/folder hover:bg-sidebar-accent/50 rounded">
            <CollapsibleTrigger asChild>
              <Button
                variant="ghost"
                className="flex-1 justify-start h-7 px-2 py-1 text-xs hover:bg-transparent group-hover/folder:bg-transparent rounded focus-visible:ring-0 focus-visible:ring-offset-0"
                onClick={() => {
                  setIsOpen(!isOpen);
                  onClick?.();
                }}
              >
                <div className="flex items-center gap-2 w-full">
                  <div className="flex items-center">
                    {hasChildren ? (
                      <div className="size-3 flex items-center justify-center">
                        <div className="group-hover/folder:hidden">
                          <Folder className="size-3 text-sidebar-foreground/60" />
                        </div>
                        <div className="hidden group-hover/folder:block">
                          {isOpen ? 
                            <ChevronDown className="size-3 text-sidebar-foreground/60" /> :
                            <ChevronRight className="size-3 text-sidebar-foreground/60" />
                          }
                        </div>
                      </div>
                    ) : (
                      <Folder className="size-3 text-sidebar-foreground/60" />
                    )}
                  </div>
                  <span className="truncate text-sidebar-foreground/80">{folder.name}</span>
                </div>
              </Button>
            </CollapsibleTrigger>
            
            {/* Actions */}
            <div className="flex items-center opacity-0 group-hover/folder:opacity-100">
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
                  <DropdownMenuItem>Rename folder</DropdownMenuItem>
                  <DropdownMenuItem>Folder settings</DropdownMenuItem>
                  <DropdownMenuItem className="text-destructive">Delete folder</DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
              
              <Button
                variant="ghost"
                size="icon"
                className="size-5 hover:bg-sidebar-accent mr-1 rounded focus-visible:ring-0 focus-visible:ring-offset-0"
                title="Add list to folder"
              >
                <Plus className="size-3" />
              </Button>
            </div>
          </div>
        </div>
        
        {/* Lists under this folder */}
        {hasChildren && (
          <CollapsibleContent className="space-y-0">
            <div className="relative">
              {/* Vertical connecting line for folder children */}
              <div className="absolute left-3.25 top-0 bottom-0 w-px bg-sidebar-border/30" />
              
              <div className="ml-4 pl-2 space-y-0">
                {folder.lists.map(list => (
                  <ListNode
                    key={list.id}
                    list={list}
                    context={{ ...folderContext, listId: list.id }}
                  />
                ))}
              </div>
            </div>
          </CollapsibleContent>
        )}
      </Collapsible>
    </div>
  );
}