import { Button } from "@/components/ui/button"
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible"
import { FolderNode } from "@/features/workspace/workspacetype"
import { cn } from "@/lib/utils"
import { Folder, ChevronRight, MoreHorizontal, Plus } from "lucide-react"
import React from "react"
import { ListContext, ListItem } from "./list-item"
import { SpaceContext } from "./space-item"

export type FolderContext = SpaceContext & {folderId: string;}
interface FolderItemProps {folder: FolderNode,context: FolderContext,isHighlighted?: boolean}

export function FolderItem({ folder,context,isHighlighted }: FolderItemProps) {
  const [isOpen, setIsOpen] = React.useState(true) // Folders are open by default in the image
  const [isHovered, setIsHovered] = React.useState(false) // Track hover state
  const listContext: ListContext = {
    ...context, // Includes spaceId and folderId
    listId: "" // Will be set per list
  };

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen}>
      <div className={cn("group flex items-center gap-2 py-1 px-2 rounded-sm cursor-pointer","hover:bg-neutral-800",
        isHighlighted && "bg-neutral-800",)}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}>
        <CollapsibleTrigger asChild>
          <div className="relative w-6 h-6 flex items-center justify-center flex-shrink-0">
            <Folder className={cn("absolute h-4 w-4 text-yellow-500 transition-opacity",isHovered ? "opacity-0" : "opacity-100",)} />
            {/* Arrow: visible ONLY when hovered, rotates if open */}
            <ChevronRight className={cn("absolute h-4 w-4 text-neutral-400 transition-transform",
                isOpen ? "rotate-90" : "", isHovered ? "opacity-100" : "opacity-0", )}/>
          </div>
        </CollapsibleTrigger>
        <span className="flex-1 text-sm font-medium text-neutral-200 truncate">{folder.name}</span>
        <div className="flex items-center gap-1">
          <Button variant="ghost" size="sm" className="h-6 w-6 p-0 text-neutral-400 hover:bg-neutral-700">
            <MoreHorizontal className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="sm" className="h-6 w-6 p-0 text-neutral-400 hover:bg-neutral-700">
            <Plus className="h-4 w-4" />
          </Button>
        </div>
      </div>
      <CollapsibleContent>
        <div className="relative pl-6 before:absolute before:left-[20px] before:top-0 before:bottom-0 before:w-px before:bg-neutral-700">
          <div className="space-y-0.5">
            {folder.lists.map((list) => (
               <ListItem 
                key={list.id} 
                list={list} 
                context={{ ...listContext, listId: list.id }} // Pass full context
                isHighlighted={isHighlighted && list.id === "list-2"} 
              />
            ))}
          </div>
        </div>
      </CollapsibleContent>
    </Collapsible>
  )
}