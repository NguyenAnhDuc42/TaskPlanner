"use client"

import * as React from "react"
import { FolderTree, ChevronDown, MoreHorizontal } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { SpaceNode } from "@/features/workspace/workspacetype"
import { SpaceItem } from "./space-item"
import { useSidebar } from "@/components/ui/sidebar"
import { cn } from "@/lib/utils"
import { CreateSpaceButton } from "../forms/create-space-form"


export function SpaceHierarchyDisplay({ spaces }: { spaces: SpaceNode[] }) {
  const [isCreateSpaceModalOpen, setIsCreateSpaceModalOpen] = React.useState(false)
  const [isExpanded, setIsExpanded] = React.useState(true)
  const { state } = useSidebar()
  const isCollapsed = state === "collapsed"

  // When collapsed, show only a single icon representing the entire hierarchy
  if (isCollapsed) {
    return (
      <div className="flex justify-center py-2">
        <Button
          variant="ghost"
          size="icon"
          className="h-9 w-9 p-0 text-sidebar-foreground/70 hover:bg-sidebar-accent hover:text-sidebar-foreground border border-transparent hover:border-sidebar-border transition-all duration-200"
          title="Workspace Navigation"
        >
          <FolderTree className="h-4 w-4" />
        </Button>
      </div>
    )
  }

  return (
    <div className="flex flex-col h-full">
      {/* Fixed Spaces Header */}
      <div className="flex-shrink-0 border-b border-sidebar-border/30">
        <div className="group flex items-center gap-2 px-2 py-1.5 cursor-pointer border border-transparent hover:bg-sidebar-accent hover:border-sidebar-border transition-all duration-200">
          <Button
            variant="ghost"
            size="sm"
            className="h-5 w-5 p-0 text-sidebar-foreground/60 hover:bg-transparent"
            onClick={() => setIsExpanded(!isExpanded)}
          >
            <ChevronDown className={cn("h-3 w-3 transition-transform duration-200", !isExpanded && "-rotate-90")} />
          </Button>

          <span className="flex-1 text-sm font-medium text-sidebar-foreground/80">Spaces</span>

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

            <CreateSpaceButton
              isOpen={isCreateSpaceModalOpen}
              onOpenChange={setIsCreateSpaceModalOpen}
              variant="header"
            />
          </div>
        </div>
      </div>

      {/* Scrollable Spaces Content */}
      {isExpanded && (
        <div className="flex-1 overflow-hidden flex flex-col">
          <div className="flex-1 overflow-y-auto modern-scrollbar px-2 py-1">
            <div className="space-y-0.5">
              {spaces.map((space) => (
                <SpaceItem key={space.id} space={space} context={{ spaceId: space.id }} />
              ))}
               <div className="flex-shrink-0 border-t border-sidebar-border/20">
                  <CreateSpaceButton
                  isOpen={isCreateSpaceModalOpen}
                  onOpenChange={setIsCreateSpaceModalOpen}
                  variant="footer"
                  />
               </div>
          </div>
          </div>
        </div>
      )}
    </div>
  )
}
