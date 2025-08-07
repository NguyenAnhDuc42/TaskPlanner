"use client";

import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarHeader,
  SidebarMenu,
  SidebarRail,
  useSidebar, // Import useSidebar hook
} from "@/components/ui/sidebar";
import { Home, Users, FolderOpen, Plus, GripVertical } from "lucide-react";
import { Button } from "@/components/ui/button"; // Import Button component
import { SidebarHeaderBrand } from "./sidebar-header-brand";
import { SidebarNavItem } from "./sidebar-nav-item";
import { SidebarUserMenu } from "./sidebar-user-menu";
import { useUser } from "@/features/auth/hooks";
import { SidebarAvatarSkeleton } from "./sidebar-skeletion";

const navigationItems = [
  {
    title: "Dashboard",
    icon: Home,
    href: "#",
    isActive: true,
  },
  {
    title: "Members",
    icon: Users,
    href: "#",
  },
  {
    title: "Spaces",
    icon: FolderOpen,
    href: "#",
  },
];

export function WorkspaceSidebar() {
  const { toggleSidebar, state } = useSidebar();
  const { data: user, error: userError, isLoading: isUserLoading } = useUser();
  
  if (isUserLoading) {
    return <SidebarAvatarSkeleton/>
  }
  
  if (userError || !user) {
    return (
      <div className="bg-black min-h-screen flex items-center justify-center text-white">
        <p>Could not load user data. Please try to login again.</p>
        {/* You can add a login button here */}
      </div>
    );
  }
 


  const handleSidebarClick = (e: React.MouseEvent) => {
    // Only expand when collapsed and not clicking on interactive elements
    if (state === "collapsed") {
      const target = e.target as HTMLElement;
      // Prevent expansion if clicking on buttons or other interactive elements
      if (target.closest("button") || target.closest("a")) {
        return;
      }
      toggleSidebar();
    }
  };

  const handleMenuItemClick = () => {
    // Expand sidebar when any menu item is clicked
    if (state === "collapsed") {
      toggleSidebar();
    }
  };

  return (
    <Sidebar
      collapsible="icon"
      className="border-r cursor-pointer data-[state=expanded]:cursor-default relative"
      onClick={handleSidebarClick} // Add onClick to sidebar for expansion
    >
      {/* GripVertical button for explicit toggle */}
      <Button
        variant="ghost"
        size="icon"
        onClick={(e) => {
          e.stopPropagation();
          toggleSidebar();
        }}
        className="h-10 w-5 absolute top-1/2 -right-2.5 z-50 bg-background/80 backdrop-blur-sm border border-border hover:bg-accent transition-all duration-200"
        title="Collapse sidebar"
      >
        <GripVertical className="size-4" />
      </Button>

      <SidebarHeader className="px-1 border-b">
        <SidebarHeaderBrand name="next15-social-app" shortName="N" />
      </SidebarHeader>

      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              {navigationItems.map((item) => (
                <SidebarNavItem
                  key={item.title}
                  title={item.title}
                  icon={item.icon}
                  href={item.href}
                  isActive={item.isActive}
                  onClick={handleMenuItemClick} // Add onClick to menu items
                />
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              <SidebarNavItem
                title="New Space"
                icon={Plus}
                href="#"
                onClick={handleMenuItemClick} // Add onClick to menu items
              />
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>

      <SidebarFooter className="px-1">
        <SidebarUserMenu
          user={user}
          onProfileSettings={() => console.log("Profile Settings")}
          onNotifications={() => console.log("Notifications")}
          onBilling={() => console.log("Billing")}
          onSignOut={() => console.log("Sign Out")}
        />
      </SidebarFooter>

      <SidebarRail />
    </Sidebar>
  );
}
