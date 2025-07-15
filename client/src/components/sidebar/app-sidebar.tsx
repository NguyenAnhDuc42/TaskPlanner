"use client";

import * as React from "react";
import { NavUser } from "@/components/sidebar/nav-user";
import { WorkspaceSwitcher } from "@/components/sidebar/workspacenav/workspace-switcher";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarHeader,
  SidebarRail,
} from "@/components/ui/sidebar";
import { SpaceHierarchyDisplay } from "./hierarchynav/nav-hierarchy";
import { useHierarchy } from "@/features/workspace/workspace-hooks";
import { useWorkspaceStore } from "@/utils/workspace-store";

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  const { selectedWorkspaceId } = useWorkspaceStore();
  const { data } = useHierarchy(selectedWorkspaceId);
  return (
    <Sidebar collapsible="icon" {...props}>
      <SidebarHeader>
        <WorkspaceSwitcher />
      </SidebarHeader>
      <SidebarContent>
        <SpaceHierarchyDisplay spaces={data?.spaces || []} />
      </SidebarContent>
      <SidebarFooter>
        <NavUser />
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  );
}
