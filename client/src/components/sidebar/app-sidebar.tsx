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
  useSidebar,
} from "@/components/ui/sidebar"
import { SpaceHierarchyDisplay } from "./hierarchynav/nav-hierarchy"
import { useHierarchy } from "@/features/workspace/workspace-hooks"
import { useWorkspaceStore } from "@/utils/workspace-store"
import { LayoutDashboard, Users } from "lucide-react"
import { cn } from "@/lib/utils"
import { SidebarNavButton } from "./navigationsbutton/sidebar-nav-button"

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  const { selectedWorkspaceId } = useWorkspaceStore()
  const { data } = useHierarchy(selectedWorkspaceId)
  const { state } = useSidebar()

  return (
    <Sidebar collapsible="icon" {...props}>
      <div className="flex flex-col h-full">
      <SidebarHeader className="p-0 m-0">
        <WorkspaceSwitcher />
      </SidebarHeader>

        <SidebarContent className="flex flex-col h-full">
          <div className="flex-shrink-0">
            <SidebarMenu className={cn("m-0 p-0", state === "collapsed" ? "px-2 gap-0" : "gap-0")}>
              <SidebarNavButton href={`/ws/${selectedWorkspaceId}`} icon={LayoutDashboard} label="Dashboard" />
              <SidebarNavButton href={`/ws/${selectedWorkspaceId}/members`} icon={Users} label="Members" />
            </SidebarMenu>
          </div>

          <div className="flex-1 overflow-auto modern-scrollbar">
            <SpaceHierarchyDisplay spaces={data?.spaces || []} />
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
