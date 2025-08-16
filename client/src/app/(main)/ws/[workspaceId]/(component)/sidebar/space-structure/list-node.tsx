import { useState, useRef, useLayoutEffect } from "react";
import { List, MoreHorizontal, Plus } from "lucide-react";

import type { ListNode as ListNodeType } from "@/features/workspace/workspacetype";
import Link from "next/link";
import { useWorkspaceStore } from "@/utils/workspace-store";
import React from "react";
import { Button } from "@/components/ui/button";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";

export type ListContext = {
  spaceId: string;
  folderId?: string;
  listId: string;
};

interface ListNodeProps {
  list: ListNodeType;
  context: ListContext;
  onClick?: () => void;
}

export function ListNode({ list, context, onClick }: ListNodeProps) {
  const { selectedWorkspaceId } = useWorkspaceStore();
  const textRef = useRef<HTMLSpanElement>(null);
  const [isOverflowing, setIsOverflowing] = useState(false);

  const listUrl = selectedWorkspaceId
    ? `/ws/${selectedWorkspaceId}/l/${context.listId}`
    : "#";

  useLayoutEffect(() => {
    const element = textRef.current;
    if (!element) return;

    const observer = new ResizeObserver(() => {
      setIsOverflowing(element.scrollWidth > element.clientWidth);
    });

    observer.observe(element);

    return () => observer.disconnect();
  }, [list.name]);

  return (
    <div className="relative">

      
      <div className="flex items-center group/list hover:bg-sidebar-accent/50 rounded">
        <div
          className="flex-1 hover:underline flex items-center gap-2 w-full justify-start h-7 px-2 py-1 text-sm rounded focus-visible:ring-0 focus-visible:ring-offset-0"
        >
          <List className="size-3 text-sidebar-foreground/60" />
          {isOverflowing ? (
            <Tooltip>
              <TooltipTrigger asChild>
                <span ref={textRef} className="block whitespace-nowrap overflow-hidden font-normal text-sidebar-foreground/80 max-w-[80px] [mask-image:linear-gradient(to_right,black_85%,transparent)]">
                  {list.name}
                </span>
              </TooltipTrigger>
              <TooltipContent side="top" align="start">
                <p>{list.name}</p>
              </TooltipContent>
            </Tooltip>
          ) : (
            <span ref={textRef} className="block whitespace-nowrap overflow-hidden font-normal text-sidebar-foreground/80 max-w-[80px]">
              <Link href={listUrl}>
              {list.name}
              </Link>
            </span>
          )}
        </div>
        
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
          
          <Button
            variant="ghost"
            size="icon"
            className="size-5 hover:bg-sidebar-accent mr-1 rounded focus-visible:ring-0 focus-visible:ring-offset-0"
            title="Add task to list"
          >
            <Plus className="size-3" />
          </Button>
        </div>
      </div>
    </div>
  );
}