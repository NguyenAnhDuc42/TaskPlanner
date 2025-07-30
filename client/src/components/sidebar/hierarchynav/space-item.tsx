"use client"

import { Button } from "@/components/ui/button"
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible"
import type { SpaceNode } from "@/features/workspace/workspacetype"
import { cn } from "@/lib/utils"
import { ChevronRight, LayoutGrid, MoreHorizontal } from "lucide-react"
import React from "react"
import { FolderItem } from "./folder-item"
import { ListItem } from "./list-item"
import { CreateFolderListButton } from "../forms/create-folder-list-form"

export type SpaceContext = { spaceId: string }
interface SpaceItemProps {
  space: SpaceNode
  context: SpaceContext
}

export function SpaceItem({ space, context }: SpaceItemProps) {
  const [isOpen, setIsOpen] = React.useState(true)
  const [isHovered, setIsHovered] = React.useState(false)
  const [showCreateModal, setShowCreateModal] = React.useState(false)

  const hasChildren = (space.folders && space.folders.length > 0) || (space.directLists && space.directLists.length > 0)
  const SpaceIcon = LayoutGrid

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen}>
      <div
        className={cn(
          "group flex items-center gap-3 py-2 px-2 rounded-md cursor-pointer transition-all duration-200",
          "border border-transparent",
          "hover:bg-sidebar-accent hover:border-sidebar-border",
        )}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      >
        <CollapsibleTrigger asChild>
          <div className="relative w-6 h-6 flex items-center justify-center flex-shrink-0">
            <SpaceIcon
              className={cn(
                "absolute h-4 w-4 text-sidebar-foreground/80 transition-all duration-200",
                isHovered ? "opacity-0 scale-90" : "opacity-100 scale-100",
              )}
            />
            {hasChildren && (
              <ChevronRight
                className={cn(
                  "absolute h-4 w-4 text-sidebar-foreground/70 transition-all duration-200",
                  isOpen ? "rotate-90" : "",
                  isHovered ? "opacity-100 scale-100" : "opacity-0 scale-90",
                )}
              />
            )}
          </div>
        </CollapsibleTrigger>

        <span className="flex-1 font-medium text-sidebar-foreground truncate text-sm">{space.name}</span>

        <div
          className={cn(
            "flex items-center gap-1 transition-all duration-200",
            "opacity-0 group-hover:opacity-100 translate-x-2 group-hover:translate-x-0",
          )}
        >
          <Button
            variant="ghost"
            size="sm"
            className="h-6 w-6 p-0 text-sidebar-foreground/60 hover:bg-sidebar-accent hover:text-sidebar-foreground transition-all duration-200"
          >
            <MoreHorizontal className="h-3 w-3" />
          </Button>

          <CreateFolderListButton
            spaceId={context.spaceId}
            spaceName={space.name}
            isOpen={showCreateModal}
            onOpenChange={setShowCreateModal}
          />
        </div>
      </div>

      {hasChildren && (
        <CollapsibleContent>
          <div className="relative pl-6 ml-3 before:absolute before:left-[12px] before:top-0 before:bottom-0 before:w-px before:bg-sidebar-border">
            <div className="space-y-0.5 pt-1">
              {/* Map folders with keys */}
              {space.folders?.map((folder) => (
                <FolderItem
                  key={`${folder.id}`}
                  folder={folder}
                  context={{
                    spaceId: context.spaceId,
                    folderId: folder.id,
                  }}
                />
              ))}
              {/* Map direct lists with keys */}
              {space.directLists?.map((list) => (
                <ListItem
                  key={`${list.id}`}
                  list={list}
                  context={{
                    spaceId: context.spaceId,
                    folderId: undefined,
                    listId: list.id,
                  }}
                />
              ))}
            </div>
          </div>
        </CollapsibleContent>
      )}
    </Collapsible>
  )
}
