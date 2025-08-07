"use client"

import { Skeleton } from "@/components/ui/skeleton"
import { useSidebar } from "@/components/ui/sidebar"
import { cn } from "@/lib/utils"



export function SidebarAvatarSkeleton() {
  const { state } = useSidebar()
  const isCollapsed = state === "collapsed"

  return (
    <div
      className={cn(
        "flex items-center gap-2 py-2 transition-all duration-200",
        isCollapsed
          ? "h-8 w-8 p-0 justify-start px-[3px]" // Collapsed: button shrinks, no padding, left-aligned with 3px padding
          : "h-12 justify-start px-[3px]" // Expanded: full height, left-aligned with 3px padding
      )}
    >
      <Skeleton className="h-8 w-8 shrink-0 rounded-lg" /> {/* Avatar skeleton */}
      {!isCollapsed && (
        <div className="flex flex-col min-w-0 flex-1">
          <Skeleton className="h-4 w-3/4 rounded-md" /> {/* Name skeleton */}
          <Skeleton className="h-3 w-1/2 rounded-md mt-1" /> {/* Email skeleton */}
        </div>
      )}
    </div>
  )
}
