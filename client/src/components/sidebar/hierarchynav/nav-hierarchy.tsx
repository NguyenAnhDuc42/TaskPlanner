"use client"

import * as React from "react"
import { Plus } from "lucide-react"

import { Button } from "@/components/ui/button"
import { SpaceNode } from "@/features/workspace/workspacetype"
import { SpaceItem } from "./space-item"

export function SpaceHierarchyDisplay({ spaces }: { spaces: SpaceNode[] }) {
  return (
    <div className="px-2 space-y-1">
      {spaces.map((space) => (
        <SpaceItem key={space.id} space={space} context={{ spaceId: space.id }} />
      ))}
      <Button variant="ghost" className="w-full justify-start mt-4 text-neutral-400 hover:bg-neutral-800 hover:text-neutral-200 py-1 px-2">
        <div className="w-6 flex-shrink-0 flex items-center justify-center">
          <Plus className="h-4 w-4" />
        </div>
        <span className="flex-1 text-sm font-medium">New Space</span>
      </Button>
    </div>
  )
}
