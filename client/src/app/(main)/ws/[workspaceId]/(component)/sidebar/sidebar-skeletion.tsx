"use client"

import { 
  Sidebar, 
  SidebarContent, 
  SidebarFooter, 
  SidebarGroup, 
  SidebarGroupContent, 
  SidebarHeader, 
  SidebarMenu, 
  SidebarMenuItem, 
  SidebarMenuSkeleton,
  SidebarRail,
  useSidebar
} from "@/components/ui/sidebar"
import { Skeleton } from "@/components/ui/skeleton"
import { cn } from "@/lib/utils"

export function SidebarSkeleton() {
  const { state } = useSidebar();
  const isCollapsed = state === "collapsed";

  return (
    <Sidebar 
      collapsible="icon" 
      className="border-r cursor-pointer data-[state=expanded]:cursor-default relative"
    >
      <SidebarHeader className="">
       <div className={cn(
          "flex items-center gap-2 py-2 transition-all duration-200",
          isCollapsed
            ? "h-8 w-8 p-0 justify-start px-[3px]"
            : "h-12 justify-start px-[3px]"
        )}>
          <Skeleton className="h-8 w-8 shrink-0 rounded-lg" /> {/* Avatar skeleton */}
          {!isCollapsed && (
            <div className="flex flex-col min-w-0 flex-1">
              <Skeleton className="h-4 w-3/4 rounded-md" /> {/* Name skeleton */}
              <Skeleton className="h-3 w-1/2 rounded-md mt-1" /> {/* Email skeleton */}
            </div>
          )}
        </div>
      </SidebarHeader>
      
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              {Array.from({ length: 3 }).map((_, index) => (
                <SidebarMenuItem key={index}>
                  <SidebarMenuSkeleton showIcon={true} />
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
        
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              <SidebarMenuItem>
                <SidebarMenuSkeleton showIcon={true} />
              </SidebarMenuItem>
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
      
      <SidebarFooter>
        {/* User Menu Skeleton */}
        <div className={cn(
          "flex items-center gap-2 py-2 transition-all duration-200",
          isCollapsed
            ? "h-8 w-8 p-0 justify-start px-[3px]"
            : "h-12 justify-start px-[3px]"
        )}>
          <Skeleton className="h-8 w-8 shrink-0 rounded-lg" /> {/* Avatar skeleton */}
          {!isCollapsed && (
            <div className="flex flex-col min-w-0 flex-1">
              <Skeleton className="h-4 w-3/4 rounded-md" /> {/* Name skeleton */}
              <Skeleton className="h-3 w-1/2 rounded-md mt-1" /> {/* Email skeleton */}
            </div>
          )}
        </div>
      </SidebarFooter>
      
      <SidebarRail />
    </Sidebar>
  )
}
