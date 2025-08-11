"use client"

import { Skeleton } from "@/components/ui/skeleton"
import {
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarHeader,
  SidebarMenu,
} from "@/components/ui/sidebar"

function SkeletonItem({ width = "w-full" }) {
  return (
    <div className="flex items-center gap-2 p-2">
      <Skeleton className="h-4 w-4" />
      <Skeleton className={`h-4 ${width}`} />
    </div>
  )
}

export function WorkspaceSidebarSkeleton() {
  return (
    <div className="w-64 h-screen bg-background border-r flex flex-col">
      <SidebarHeader className="px-1 border-b bg-background border-border">
        <div className="flex items-center gap-2 p-2">
          <Skeleton className="h-8 w-8 rounded-md" />
          <div className="flex-1 space-y-1.5">
            <Skeleton className="h-4 w-3/4" />
            <Skeleton className="h-3 w-1/2" />
          </div>
        </div>
      </SidebarHeader>

      <SidebarContent className="bg-background">
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              <SkeletonItem width="w-24" />
              <SkeletonItem width="w-20" />
            </SidebarMenu>
            <div className="mt-4 space-y-2">
              <SkeletonItem width="w-32" />
              <div className="ml-6 pl-2 space-y-2 border-l border-border/30">
                <SkeletonItem width="w-28" />
                <SkeletonItem width="w-24" />
              </div>
              <SkeletonItem width="w-28" />
            </div>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>

      <SidebarFooter className="px-1 border-t bg-background border-border">
        <div className="flex items-center gap-2 p-2">
          <Skeleton className="h-8 w-8 rounded-md" />
          <div className="flex-1 space-y-1.5">
            <Skeleton className="h-4 w-24" />
            <Skeleton className="h-3 w-32" />
          </div>
        </div>
      </SidebarFooter>
    </div>
  )
}