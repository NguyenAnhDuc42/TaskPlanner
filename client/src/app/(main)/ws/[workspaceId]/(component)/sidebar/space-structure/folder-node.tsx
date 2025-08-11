import { useState, useRef, useLayoutEffect } from "react";
import { Folder, MoreHorizontal, Plus, ChevronRight, ChevronDown } from "lucide-react";

import type { FolderNode as FolderNodeType } from "@/features/workspace/workspacetype";
import { ListNode } from "./list-node";
import React from "react";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import { Button } from "@/components/ui/button";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/utils";
import { useWorkspaceId } from "@/utils/currrent-layer-id";
import Link from "next/link";

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
  const workspaceId = useWorkspaceId();
  const [isOpen, setIsOpen] = useState(false);
  const textRef = useRef<HTMLSpanElement>(null);
  const [isOverflowing, setIsOverflowing] = useState(false);
  const hasChildren = folder.lists.length > 0;
  const folderContext: FolderContext = { ...context, folderId: folder.id };
  const folderUrl = workspaceId ? `/ws/${workspaceId}/folder/${folder.id}` : "#";

  useLayoutEffect(() => {
    const element = textRef.current;
    if (!element) return;

    const observer = new ResizeObserver(() => {
      setIsOverflowing(element.scrollWidth > element.clientWidth);
    });
    observer.observe(element);

    return () => observer.disconnect();
  }, [folder.name]);

  const TriggerButton = (
    <CollapsibleTrigger asChild>
      <Button
        variant="ghost"
        className="flex-1 justify-start h-7 px-2 py-1 text-sm rounded focus-visible:ring-0 focus-visible:ring-offset-0"
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
                <div className="hidden hover:bg-accent group-hover/folder:block">
                  {isOpen ? (
                    <ChevronDown className="size-3 text-sidebar-foreground/60" />
                  ) : (
                    <ChevronRight className="size-3 text-sidebar-foreground/60" />
                  )}
                </div>
              </div>
            ) : (
              <Folder className="size-3 text-sidebar-foreground/60" />
            )}
          </div>
          <span
            ref={textRef}
            className={cn(
              "block whitespace-nowrap overflow-hidden font-normal text-sidebar-foreground/80",
              isOverflowing && "[mask-image:linear-gradient(to_right,black_85%,transparent)]"
            )}
          >
            <Link href={folderUrl} className="hover:underline">
            {folder.name}
            </Link>
          </span>
        </div>
      </Button>
    </CollapsibleTrigger>
  );

  return (
    <div className="space-y-0">
      <Collapsible open={isOpen} onOpenChange={setIsOpen}>
        <div className="relative">
          <div className="flex items-center group/folder rounded">
            {isOverflowing ? (
              <Tooltip>
                <TooltipTrigger asChild>{TriggerButton}</TooltipTrigger>
                <TooltipContent side="top" align="start">
                  <p>{folder.name}</p>
                </TooltipContent>
              </Tooltip>
            ) : (
              TriggerButton
            )}

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