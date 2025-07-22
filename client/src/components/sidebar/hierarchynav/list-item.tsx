"use client"

import { Button } from "@/components/ui/button"
import type { ListNode } from "@/features/workspace/workspacetype"
import { cn } from "@/lib/utils"
import { List, MoreHorizontal } from "lucide-react"
import React from "react"
import { useWorkspaceStore } from "@/utils/workspace-store"
import Link from "next/link"
import { CreateTaskButton } from "../forms/create-task-form"


export type ListContext = {
  spaceId: string
  folderId?: string
  listId: string
}

interface ListItemProps {
  list: ListNode
  context: ListContext
  isHighlighted?: boolean
}

export function ListItem({ list, context, isHighlighted }: ListItemProps) {
  const { selectedWorkspaceId } = useWorkspaceStore()
  const [showCreateTaskModal, setShowCreateTaskModal] = React.useState(false)

  const listUrl = selectedWorkspaceId ? `/ws/${selectedWorkspaceId}/l/${context.listId}` : "#"

  return (
    <div
      className={cn(
        "group flex items-center gap-3 py-1.5 px-3 rounded-md cursor-pointer transition-all duration-200",
        "border border-transparent",
        "hover:bg-sidebar-accent/40 hover:border-sidebar-border/50",
        isHighlighted && "bg-sidebar-accent/60 border-sidebar-border/70",
      )}
    >
      <div className="w-5 flex-shrink-0 flex items-center justify-center">
        <List className="h-3 w-3 text-sidebar-foreground/60 transition-colors duration-200 group-hover:text-sidebar-foreground/80" />
      </div>

      <Link href={listUrl} className="flex flex-1 items-center gap-2 min-w-0" passHref>
        <span className="flex-1 text-sm text-sidebar-foreground/70 truncate group-hover:text-sidebar-foreground/90 transition-colors duration-200">
          {list.name}
        </span>
      </Link>

      <div
        className={cn(
          "flex items-center gap-1 transition-all duration-200",
          "opacity-0 group-hover:opacity-100 translate-x-2 group-hover:translate-x-0",
        )}
      >
        <Button
          variant="ghost"
          size="sm"
          className="h-5 w-5 p-0 text-sidebar-foreground/40 hover:bg-sidebar-accent hover:text-sidebar-foreground transition-all duration-200"
        >
          <MoreHorizontal className="h-3 w-3" />
        </Button>

        <CreateTaskButton
          listContext={context}
          listName={list.name}
          isOpen={showCreateTaskModal}
          onOpenChange={setShowCreateTaskModal}
        />
      </div>
    </div>
  )
}
