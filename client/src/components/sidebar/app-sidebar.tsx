"use client"

import type * as React from "react"
import { NavUser } from "@/components/sidebar/nav-user"
import { WorkspaceSwitcher } from "@/components/sidebar/workspacenav/workspace-switcher"
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarHeader,
  SidebarRail,
  SidebarMenu,
  SidebarMenuItem,
  SidebarMenuButton,
  useSidebar,
} from "@/components/ui/sidebar"
import { Button } from "@/components/ui/button"
import { SpaceHierarchyDisplay } from "./hierarchynav/nav-hierarchy"
import { useHierarchy } from "@/features/workspace/workspace-hooks"
import { useWorkspaceStore } from "@/utils/workspace-store"
import { LayoutDashboard, Briefcase, Users } from "lucide-react"
import { cn } from "@/lib/utils"

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  const { selectedWorkspaceId } = useWorkspaceStore()
  const { data } = useHierarchy(selectedWorkspaceId)
  const { state } = useSidebar()

  return (
    <Sidebar collapsible="icon" {...props}>
      <div className="flex flex-col h-full py-2">
      <SidebarHeader className="p-0 mb-3">
        <WorkspaceSwitcher />
      </SidebarHeader>

      <SidebarContent className="flex flex-col h-full">
        <div className="flex-shrink-0">
          <SidebarMenu className={cn("m-0 p-0", state === "collapsed" ? "px-2 gap-1" : "gap-0")}>
            <SidebarMenuItem className="m-0 p-0">
              <SidebarMenuButton asChild
                className={cn(
                  "transition-all duration-200 m-0",
                  state === "collapsed" ? "rounded-lg size-8 p-0 justify-center my-3" : "rounded-none",
                )}
              >
                <Button
                  variant="ghost"
                  className="w-full h-auto p-2 justify-start border border-transparent hover:bg-sidebar-accent hover:border-sidebar-border transition-all duration-200"
                >
                  <LayoutDashboard className={state === "collapsed" ? "h-5 w-5" : "h-4 w-4"} />
                  {state !== "collapsed" && <span>DashBoard</span>}
                </Button>
              </SidebarMenuButton>
            </SidebarMenuItem>
            <SidebarMenuItem className="m-0 p-0">
              <SidebarMenuButton asChild
                className={cn(
                  "transition-all duration-200 m-0",
                  state === "collapsed" ? "rounded-lg size-8 p-0 justify-center my-3" : "rounded-none",
                )}
              >
                <Button
                  variant="ghost"
                  className="w-full h-auto p-2 justify-start border border-transparent hover:bg-sidebar-accent hover:border-sidebar-border transition-all duration-200"
                >
                  <Briefcase className={state === "collapsed" ? "h-5 w-5" : "h-4 w-4"} />
                  {state !== "collapsed" && <span>MyWork</span>}
                </Button>
              </SidebarMenuButton>
            </SidebarMenuItem>
          </SidebarMenu>
        </div>

        <div className="flex-1 overflow-auto modern-scrollbar">
          <SpaceHierarchyDisplay spaces={data?.spaces || []} />
        </div>

        <div className="flex-shrink-0 mt-auto">
          <SidebarMenu className={cn("m-0 p-0", state === "collapsed" ? "px-2 gap-1" : "gap-0")}>
            <SidebarMenuItem className="m-0 p-0">
              <SidebarMenuButton
                asChild
                className={cn("transition-all duration-200 m-0",
                  state === "collapsed" ? "rounded-lg size-8 p-0 justify-center my-2" : "rounded-none",)}>
                <Button
                  variant="ghost"
                  className="w-full h-auto p-2 justify-start border border-transparent hover:bg-sidebar-accent hover:border-sidebar-border transition-all duration-200"
                >
                  <Users className={state === "collapsed" ? "h-5 w-5" : "h-4 w-4"} />
                  {state !== "collapsed" && <span>Members</span>}
                </Button>
              </SidebarMenuButton>
            </SidebarMenuItem>
          </SidebarMenu>
        </div>
      </SidebarContent>

      <SidebarFooter className="p-0">
        <NavUser />
      </SidebarFooter>
      <SidebarRail />
      </div>
    </Sidebar>
  )
}
