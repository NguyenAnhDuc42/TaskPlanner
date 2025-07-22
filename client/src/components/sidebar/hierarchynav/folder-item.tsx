"use client"

import { Button } from "@/components/ui/button"
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible"
import type { FolderNode } from "@/features/workspace/workspacetype"
import { cn } from "@/lib/utils"
import { Folder, ChevronRight, MoreHorizontal } from "lucide-react"
import React from "react"
import { type ListContext, ListItem } from "./list-item"
import type { SpaceContext } from "./space-item"
import { CreateListButton } from "../forms/create-list-form"


export type FolderContext = SpaceContext & { folderId: string }
interface FolderItemProps {
  folder: FolderNode
  context: FolderContext
  isHighlighted?: boolean
}

export function FolderItem({ folder, context, isHighlighted }: FolderItemProps) {
  const [isOpen, setIsOpen] = React.useState(true)
  const [isHovered, setIsHovered] = React.useState(false)
  const [showCreateListModal, setShowCreateListModal] = React.useState(false)

  const listContext: ListContext = {
    ...context,
    listId: "",
  }

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen}>
      <div
        className={cn(
          "group flex items-center gap-3 py-1.5 px-3 rounded-md cursor-pointer transition-all duration-200",
          "border border-transparent",
          "hover:bg-sidebar-accent/60 hover:border-sidebar-border/70",
          isHighlighted && "bg-sidebar-accent border-sidebar-border",
        )}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      >
        <CollapsibleTrigger asChild>
          <div className="relative w-5 h-5 flex items-center justify-center flex-shrink-0">
            <Folder
              className={cn(
                "absolute h-4 w-4 text-sidebar-foreground/70 transition-all duration-200",
                isHovered ? "opacity-0 scale-90" : "opacity-100 scale-100",
              )}
            />
            <ChevronRight
              className={cn(
                "absolute h-4 w-4 text-sidebar-foreground/60 transition-all duration-200",
                isOpen ? "rotate-90" : "",
                isHovered ? "opacity-100 scale-100" : "opacity-0 scale-90",
              )}
            />
          </div>
        </CollapsibleTrigger>

        <span className="flex-1 text-sm text-sidebar-foreground/90 truncate">{folder.name}</span>

        <div
          className={cn(
            "flex items-center gap-1 transition-all duration-200",
            "opacity-0 group-hover:opacity-100 translate-x-2 group-hover:translate-x-0",
          )}
        >
          <Button
            variant="ghost"
            size="sm"
            className="h-5 w-5 p-0 text-sidebar-foreground/50 hover:bg-sidebar-accent hover:text-sidebar-foreground transition-all duration-200"
          >
            <MoreHorizontal className="h-3 w-3" />
          </Button>

          <CreateListButton
            folderContext={context}
            folderName={folder.name}
            isOpen={showCreateListModal}
            onOpenChange={setShowCreateListModal}
          />
        </div>
      </div>

      <CollapsibleContent>
        <div className="relative pl-5 ml-3 before:absolute before:left-[10px] before:top-0 before:bottom-0 before:w-px before:bg-sidebar-border/60">
          <div className="space-y-0.5 pt-0.5">
            {folder.lists.map((list) => (
              <ListItem
                key={list.id}
                list={list}
                context={{ ...listContext, listId: list.id }}
                isHighlighted={isHighlighted && list.id === "list-2"}
              />
            ))}
          </div>
        </div>
      </CollapsibleContent>
    </Collapsible>
  )
}
