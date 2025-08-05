"use client"
import { Sidebar, SidebarContent, SidebarFooter, SidebarGroup, SidebarGroupContent, SidebarHeader, SidebarMenu, SidebarMenuButton, SidebarMenuItem, SidebarRail, useSidebar } from "@/components/ui/sidebar";
import { 
  Home, 
  Users, 
  FolderOpen, 
  Plus,
  GripVertical
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { useUser } from "@/features/auth/hooks";
import { UserMenuBar } from "@/components/auth/user-menu-bar";
import { toast } from "sonner";


const navigationItems = [
  {
    title: "Dashboard",
    icon: Home,
    href: "#"
  },
  {
    title: "Members", 
    icon: Users,
    href: "#"
  },
  {
    title: "Spaces",
    icon: FolderOpen,
    href: "#"
  }
];

export function AppSidebar() {
  const { toggleSidebar, state } = useSidebar();
  const { data: user, error: userError, isLoading: isUserLoading } = useUser();
  if (isUserLoading) {
    toast.loading("Loading user data...");
    return null;
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
      // Prevent expansion if clicking on buttons or other interactive elements
      const target = e.target as HTMLElement;
      if (target.closest('button') || target.closest('a')) {
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
      onClick={handleSidebarClick}
    >
     <Button variant="ghost" size="icon" onClick={(e) => { e.stopPropagation(); toggleSidebar();}}
      className="h-10 w-5 absolute top-1/2 -right-2.5 z-50 bg-background/80 backdrop-blur-sm border border-border hover:bg-accent transition-all duration-200"
      title="Collapse sidebar" >
            <GripVertical className="size-4" />
      </Button>
      <SidebarHeader className="border-b border-sidebar-border">
        <div className="flex items-center gap-2 px-1 py-1.5">
          <div className="flex items-center justify-center size-8 bg-primary text-primary-foreground rounded-lg">
            <span className="font-medium">N</span>
          </div>
          <div className="grid flex-1 text-left text-sm leading-tight">
            <span className="truncate font-semibold">next15-social-app</span>
          </div>
        </div>
      </SidebarHeader>
      
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              {navigationItems.map((item) => (
                <SidebarMenuItem key={item.title}>
                  <SidebarMenuButton 
                    tooltip={item.title}
                    onClick={handleMenuItemClick}
                  >
                    <item.icon />
                    <span>{item.title}</span>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
        
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              <SidebarMenuItem>
                <SidebarMenuButton 
                  tooltip="New Space"
                  onClick={handleMenuItemClick}
                >
                  <Plus />
                  <span>New Space</span>
                </SidebarMenuButton>
              </SidebarMenuItem>
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
      
        <SidebarFooter className="border-t border-sidebar-border p-0">
        <SidebarMenuButton onClick={handleMenuItemClick} className="h-13 rounded-none">
             <UserMenuBar currentUser={user} isCollapsed={state === 'collapsed'} />
        </SidebarMenuButton>
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  );
}