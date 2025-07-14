"use client"

import * as React from "react"


import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"
import { ChevronRight, Folder, List, MoreHorizontal, Plus, Square } from "lucide-react"

// User-provided interfaces
export interface Hierarchy {
  spaces: SpaceNode[]
}

export interface SpaceNode {
  id: string
  name: string
  lists: ListNode[] | null
  folders: FolderNode[] | null
}

export interface FolderNode {
  id: string
  name: string
  lists: ListNode[]
}

export interface ListNode {
  id: string
  name: string
}

// Sample data strictly adhering to the provided interfaces
const sampleHierarchyData: Hierarchy = {
  spaces: [
    {
      id: "space-all-tasks",
      name: "All Tasks - Anh Đức Nguyễn's...",
      lists: null,
      folders: null,
    },
    {
      id: "space-team",
      name: "Team Space",
      lists: null,
      folders: null,
    },
    {
      id: "space-main",
      name: "Space",
      lists: [{ id: "list-3", name: "List" }],
      folders: [
        {
          id: "folder-1",
          name: "Folder",
          lists: [{ id: "list-1", name: "List" }],
        },
        {
          id: "folder-2",
          name: "Folder", // This one is highlighted in the image
          lists: [{ id: "list-2", name: "List" }], // This list is also highlighted
        },
      ],
    },
  ],
}

interface ListItemProps {
  list: ListNode
  isHighlighted?: boolean
}

function ListItem({ list, isHighlighted }: ListItemProps) {
  return (
    <div
      className={cn(
        "group flex items-center gap-2 py-1 px-2 rounded-sm cursor-pointer",
        "hover:bg-neutral-800",
        isHighlighted && "bg-neutral-800", // Apply highlight if true
      )}
    >
      <div className="w-6 flex-shrink-0 flex items-center justify-center">
        {/* Indentation placeholder for the line */}
        <List className="h-4 w-4 text-gray-400" />
      </div>
      <span className="flex-1 text-sm text-neutral-200 truncate">{list.name}</span>
      <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
        <Button variant="ghost" size="sm" className="h-6 w-6 p-0 text-neutral-400 hover:bg-neutral-700">
          <MoreHorizontal className="h-4 w-4" />
        </Button>
        <Button variant="ghost" size="sm" className="h-6 w-6 p-0 text-neutral-400 hover:bg-neutral-700">
          <Plus className="h-4 w-4" />
        </Button>
      </div>
    </div>
  )
}

interface FolderItemProps {
  folder: FolderNode
  isHighlighted?: boolean
}

function FolderItem({ folder, isHighlighted }: FolderItemProps) {
  const [isOpen, setIsOpen] = React.useState(true) // Folders are open by default in the image

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen}>
      <div
        className={cn(
          "group flex items-center gap-2 py-1 px-2 rounded-sm cursor-pointer",
          "hover:bg-neutral-800",
          isHighlighted && "bg-neutral-800", // Apply highlight if true
        )}
      >
        <CollapsibleTrigger asChild>
          <div className="relative w-6 h-6 flex items-center justify-center flex-shrink-0">
            {/* Icon: visible when closed and not hovered, hidden when open or hovered */}
            <Folder
              className={cn(
                "absolute h-4 w-4 text-yellow-500 transition-opacity",
                isOpen ? "opacity-0" : "opacity-100 group-hover:opacity-0",
              )}
            />
            {/* Arrow: visible when open, or when closed and hovered */}
            <ChevronRight
              className={cn(
                "absolute h-4 w-4 text-neutral-400 transition-transform transition-opacity",
                isOpen ? "rotate-90 opacity-100" : "opacity-0 group-hover:opacity-100",
              )}
            />
          </div>
        </CollapsibleTrigger>
        <span className="flex-1 text-sm font-medium text-neutral-200 truncate">{folder.name}</span>
        <div className="flex items-center gap-1">
          {" "}
          {/* Always visible for folders */}
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
              <ListItem key={list.id} list={list} isHighlighted={isHighlighted && list.id === "list-2"} />
            ))}
          </div>
        </div>
      </CollapsibleContent>
    </Collapsible>
  )
}

interface SpaceItemProps {
  space: SpaceNode
}

function SpaceItem({ space }: SpaceItemProps) {
  const [isOpen, setIsOpen] = React.useState(true) // Spaces are open by default in the image

  const hasChildren = (space.folders && space.folders.length > 0) || (space.lists && space.lists.length > 0)

  // Determine icon and color based on name or a generic default, as per strict type adherence
  const getSpaceVisuals = (name: string) => {
    switch (name) {
      case "All Tasks - Anh Đức Nguyễn's...":
        return <Square className="h-4 w-4 flex-shrink-0 text-purple-500 transition-opacity" />
      case "Team Space":
        return <Square className="h-4 w-4 flex-shrink-0 text-blue-500 transition-opacity" />
      case "Space":
        return (
          <div className="relative h-4 w-4 flex-shrink-0 rounded-sm flex items-center justify-center text-xs font-bold text-white bg-blue-500 transition-opacity">
            S
          </div>
        )
      default:
        return <Square className="h-4 w-4 flex-shrink-0 text-gray-400 transition-opacity" />
    }
  }

  const spaceIconElement = getSpaceVisuals(space.name)

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen}>
      <div className="group flex items-center gap-2 py-1 px-2 rounded-sm cursor-pointer hover:bg-neutral-800">
        {/* Combined icon and arrow container */}
        <CollapsibleTrigger asChild>
          <div className="relative w-6 h-6 flex items-center justify-center flex-shrink-0">
            {/* Icon: visible when closed and not hovered, hidden when open or hovered */}
            {React.cloneElement(spaceIconElement, {
              className: cn(
                spaceIconElement.props.className,
                isOpen ? "opacity-0" : "opacity-100 group-hover:opacity-0",
              ),
            })}
            {/* Arrow: visible when open, or when closed and hovered */}
            {hasChildren && (
              <ChevronRight
                className={cn(
                  "absolute h-4 w-4 text-neutral-400 transition-transform transition-opacity",
                  isOpen ? "rotate-90 opacity-100" : "opacity-0 group-hover:opacity-100",
                )}
              />
            )}
          </div>
        </CollapsibleTrigger>

        <span className="flex-1 font-semibold text-neutral-200 truncate">{space.name}</span>
        <div className="flex items-center gap-1">
          {" "}
          {/* Always visible for spaces */}
          <Button variant="ghost" size="sm" className="h-6 w-6 p-0 text-neutral-400 hover:bg-neutral-700">
            <MoreHorizontal className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="sm" className="h-6 w-6 p-0 text-neutral-400 hover:bg-neutral-700">
            <Plus className="h-4 w-4" />
          </Button>
        </div>
      </div>
      {hasChildren && (
        <CollapsibleContent>
          <div className="relative pl-6 before:absolute before:left-[20px] before:top-0 before:bottom-0 before:w-px before:bg-neutral-700">
            <div className="space-y-0.5">
              {space.folders?.map((folder) => (
                <FolderItem key={folder.id} folder={folder} isHighlighted={folder.id === "folder-2"} />
              ))}
              {space.lists?.map((list) => (
                <ListItem key={list.id} list={list} />
              ))}
            </div>
          </div>
        </CollapsibleContent>
      )}
    </Collapsible>
  )
}

export function SpaceHierarchyDisplay() {
  return (
    <div className="space-y-1">
      {sampleHierarchyData.spaces.map((space) => (
        <SpaceItem key={space.id} space={space} />
      ))}
      {/* + New Space button */}
      <Button
        variant="ghost"
        className="w-full justify-start mt-4 text-neutral-400 hover:bg-neutral-800 hover:text-neutral-200 py-1 px-2"
      >
        <div className="w-6 flex-shrink-0 flex items-center justify-center">
          <Plus className="h-4 w-4" />
        </div>
        <span className="flex-1 text-sm font-medium">New Space</span>
      </Button>
    </div>
  )
}
