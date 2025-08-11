import { useState, useRef, useLayoutEffect } from "react";
import { ChevronRight, ChevronDown, MoreHorizontal, Plus  } from "lucide-react";
import { Button } from "@/components/ui/button"; // Original import path
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible"; // Original import path
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"; // Original import path
import type { SpaceNode as SpaceNodeType } from "@/features/workspace/workspacetype";
import { FolderNode } from "./folder-node";
import { ListNode } from "./list-node";
import { cn } from "@/lib/utils";
import { useWorkspaceId } from "@/utils/currrent-layer-id";
import Link from "next/link";
import React from "react";

export type SpaceContext = {
  spaceId: string;
};

interface SpaceNodeProps {
  space: SpaceNodeType;
  onClick?: () => void;
}

export function SpaceNode({ space, onClick }: SpaceNodeProps) {
  const workspaceId = useWorkspaceId();
  const [isOpen, setIsOpen] = useState(false);
  const textRef = useRef<HTMLSpanElement>(null);
  const [isOverflowing, setIsOverflowing] = useState(false);
  
  const directLists = space.directLists || [];
  const folders = space.folders || [];
  const hasChildren = folders.length > 0 || directLists.length > 0;
  const spaceContext: SpaceContext = { spaceId: space.id };
  const spaceUrl = workspaceId ? `/ws/${workspaceId}/s/${space.id}` : "#";

  useLayoutEffect(() => {
    const element = textRef.current;
    if (!element) return;

    const observer = new ResizeObserver(() => {
      setIsOverflowing(element.scrollWidth > element.clientWidth);
    });

    observer.observe(element);

    return () => observer.disconnect();
  }, [space.name]); // Re-check if the name changes

  const TriggerButton = (
    <CollapsibleTrigger asChild>
      <Button
        variant="ghost"
        className="flex-1 justify-start h-7 px-2 py-1 text-sm hover:bg-transparent group-hover/item:bg-transparent rounded focus-visible:ring-0 focus-visible:ring-offset-0"
        onClick={() => {
          setIsOpen(!isOpen);
          onClick?.();
        }}
      >
        <div className="flex items-center gap-1.5 w-full">
          <div className="flex items-center">
            {/* Icon that transforms to arrow on hover when has children */}
            {hasChildren ? (
              <div className="size-4 flex items-center justify-center">
                {/* Show arrow on hover, icon by default */}
                <div className="group-hover/item:hidden">
                  <div
                    className="size-4 rounded text-xs flex items-center justify-center text-white font-medium"
                    style={{ backgroundColor: space.color }}
                  >
                    {space.icon}
                  </div>
                </div>
                <div className="hidden hover:bg-accent group-hover/item:block">
                  {isOpen ? (
                    <ChevronDown className="size-3 text-sidebar-foreground/60" />
                  ) : (
                    <ChevronRight className="size-3 text-sidebar-foreground/60" />
                  )}
                </div>
              </div>
            ) : (
              /* If no children, just show the icon normally */
              <div
                className="size-4 rounded text-xs flex items-center justify-center text-white font-medium"
                style={{ backgroundColor: space.color }}
              >
                {space.icon}
              </div>
            )}
          </div>
          <span
            ref={textRef}
            className={cn(
              "block whitespace-nowrap overflow-hidden font-normal text-sidebar-foreground",
              isOverflowing && "[mask-image:linear-gradient(to_right,black_85%,transparent)]"
            )}
          >
            <Link href={spaceUrl} className="hover:underline">
              {space.name}
            </Link>
          </span>
        </div>
      </Button>
    </CollapsibleTrigger>
  );

  return (
    <div className="group">
      <Collapsible open={isOpen} onOpenChange={setIsOpen}>
        <div className="flex items-center group/item hover:bg-sidebar-accent/50 rounded">
          {isOverflowing ? (
            <Tooltip>
              <TooltipTrigger asChild>{TriggerButton}</TooltipTrigger>
              <TooltipContent side="top" align="start">
                <p>{space.name}</p>
              </TooltipContent>
            </Tooltip>
          ) : (
            TriggerButton
          )}

          {/* Actions - 3 dots and plus */}
          <div className="flex items-center opacity-0 group-hover/item:opacity-100">
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="ghost"
                  size="icon"
                  className="size-6 hover:bg-sidebar-accent rounded focus-visible:ring-0 focus-visible:ring-offset-0"
                >
                  <MoreHorizontal className="size-3" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start" className="w-48">
                <DropdownMenuItem>Rename space</DropdownMenuItem>
                <DropdownMenuItem>Space settings</DropdownMenuItem>
                <DropdownMenuItem>Duplicate space</DropdownMenuItem>
                <DropdownMenuItem className="text-destructive">Delete space</DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
            
            <Button
              variant="ghost"
              size="icon"
              className="size-6 hover:bg-sidebar-accent mr-1 rounded focus-visible:ring-0 focus-visible:ring-offset-0"
              title="Add new item"
            >
              <Plus className="size-3" />
            </Button>
          </div>
        </div>
        
        {hasChildren && (
          <CollapsibleContent className="space-y-0">
            <div className="relative">
              {/* Vertical connecting line - positioned to align with center of space icon */}
              <div className="absolute left-3.75 top-0 bottom-0 w-px bg-sidebar-border/30" />
              
              <div className="ml-6 pl-2 space-y-0">
                {/* Folders */}
                {folders.map(folder => (
                  <FolderNode
                    key={folder.id}
                    folder={folder}
                    context={spaceContext}
                  />
                ))}
                
                {/* Direct lists (not in folders) */}
                {directLists.map(list => (
                  <ListNode
                    key={list.id}
                    list={list}
                    context={{ ...spaceContext, listId: list.id }}
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