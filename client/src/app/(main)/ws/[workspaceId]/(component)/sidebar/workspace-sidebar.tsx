"use client";

import { useEffect, useState } from "react";
import { usePathname } from "next/navigation";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarHeader,
  SidebarMenu,
  SidebarRail,
  useSidebar,
} from "@/components/ui/sidebar";
import { Home, Users, GripVertical } from "lucide-react";
import { Button } from "@/components/ui/button";
import { SidebarNavItem } from "./sidebar-nav-item";
import { SidebarUserMenu } from "./sidebar-user-menu";
import { useLogout, useUser } from "@/features/auth/hooks";
import { WorkspaceSwitcher } from "./workspace-switcher";
import { useHierarchy, useSidebarWorkspaces } from "@/features/workspace/workspace-hooks";
import { useWorkspaceId } from "../../../../../../utils/currrent-layer-id";
import { Structure } from "./space-structure/structure-holder";
import { WorkspaceSidebarSkeleton } from "./workspace-sidebar-skeleton";

export function WorkspaceSidebar() {
  const [isClient, setIsClient] = useState(false);
  useEffect(() => {
    setIsClient(true);
  }, []);

  const workspaceId = useWorkspaceId();
  const pathname = usePathname();
  const { toggleSidebar, state } = useSidebar();
  const { data: user, error: userError, isLoading: isUserLoading } = useUser();
  const {data : workspace ,error : workspaceError, isLoading : isWorkspaceLoading} = useSidebarWorkspaces(workspaceId);
  const { data: hierarchy, error: hierarchyError, isLoading: isHierarchyLoading } = useHierarchy(workspaceId);
  const { mutate: logout } = useLogout();
  
  const handleSidebarClick = (e: React.MouseEvent) => {
    if (state === "collapsed") {
      const target = e.target as HTMLElement;
      if (target.closest("button") || target.closest("a")) {
        return;
      }
      toggleSidebar();
    }
  };

  const handleMenuItemClick = () => {
    if (state === "collapsed") {
      toggleSidebar();
    }
  };

  if (!isClient || isUserLoading || isWorkspaceLoading || isHierarchyLoading) {
    return <WorkspaceSidebarSkeleton />;
  }

  if (userError || !user) {
    return (
      <div className="bg-black min-h-screen flex items-center justify-center text-white">
        <p>Could not load user data. Please try to login again.</p>
      </div>
    );
  }
  if (workspaceError || !workspace || hierarchyError || !hierarchy) {
    return (
      <div className="p-4 text-red-500">
        <p>Error: Could not load workspace data.</p>
      </div>
    )
  }
  
 
  return (
    <Sidebar
      collapsible="icon"
      className="bg-background border-r cursor-pointer data-[state=expanded]:cursor-default relative"
      onClick={handleSidebarClick}
    >
      {/* GripVertical button for explicit toggle */}
      <Button variant="ghost" size="icon"
        onClick={(e) => {
          e.stopPropagation();
          toggleSidebar();
        }}
        className="h-12 w-3 absolute top-1/2 -right-[6.50px] z-50 rounded-sm bg-background/80 backdrop-blur-sm border border-border hover:bg-accent transition-all duration-200"
        title="Collapse sidebar" >
        <GripVertical className="size-4" />
      </Button>

     <SidebarHeader className="px-1 border-b border-border hover:bg-accent transition-colors bg-background">
        <WorkspaceSwitcher data={workspace} isLoading={isWorkspaceLoading} />
      </SidebarHeader>

      <SidebarContent className="bg-background">
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu className="mb-1">
              <SidebarNavItem
                title="Dashboard"
                icon={Home}
                href={`/ws/${workspaceId}`}
                isActive={pathname === `/ws/${workspaceId}`}
                onClick={handleMenuItemClick}
              />
              <SidebarNavItem
                title="Members"
                icon={Users}
                href={`/ws/${workspaceId}/members`}
                isActive={pathname === `/ws/${workspaceId}/members`}
                onClick={handleMenuItemClick}
              />
            </SidebarMenu>
            <Structure spaces={hierarchy.spaces} isSidebarCollapsed={state === "collapsed"} />
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>

      <SidebarFooter className="px-1 border-t border-border hover:bg-accent transition-colors bg-background">
        <SidebarUserMenu
          user={user}
          onProfileSettings={() => console.log("Profile Settings")}
          onNotifications={() => console.log("Notifications")}
          onBilling={() => console.log("Billing")}
          onSignOut={() => logout()}
        />
      </SidebarFooter>

      <SidebarRail />
    </Sidebar>
  );
}