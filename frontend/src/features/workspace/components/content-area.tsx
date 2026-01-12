"use client";

import { useSidebarContext } from "./sidebar-provider";
import { ContentSidebar } from "./content-sidebar";
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
} from "@/components/ui/sidebar";
import { Button } from "@/components/ui/button";
import { ChevronLeft, Home, Folder, FileText } from "lucide-react";

// Mock navigation items - these would be different for each content page
const getNavigationItems = (contentType: string) => {
  switch (contentType) {
    case "dashboard":
      return [
        { id: "overview", icon: Home, label: "Overview" },
        { id: "analytics", icon: FileText, label: "Analytics" },
        { id: "reports", icon: Folder, label: "Reports" },
      ];
    case "tasks":
      return [
        { id: "all-tasks", icon: FileText, label: "All Tasks" },
        { id: "my-tasks", icon: Home, label: "My Tasks" },
        { id: "completed", icon: Folder, label: "Completed" },
      ];
    case "members":
      return [
        { id: "all-members", icon: Home, label: "All Members" },
        { id: "teams", icon: Folder, label: "Teams" },
        { id: "roles", icon: FileText, label: "Roles" },
      ];
    default:
      return [
        { id: "item-1", icon: Home, label: "Item 1" },
        { id: "item-2", icon: Folder, label: "Item 2" },
      ];
  }
};

export function ContentArea() {
  const {
    activeContent,
    isInnerSidebarOpen,
    setIsInnerSidebarOpen,
    toggleInnerSidebar,
  } = useSidebarContext();
  const navItems = getNavigationItems(activeContent);

  return (
    <div className="flex-1 overflow-hidden bg-background border rounded-xl shadow-lg">
      <SidebarProvider
        open={isInnerSidebarOpen}
        onOpenChange={setIsInnerSidebarOpen}
      >
        <Sidebar>
          <SidebarHeader className="flex flex-row items-center justify-between p-4">
            <h2 className="font-semibold capitalize">{activeContent}</h2>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8"
              onClick={toggleInnerSidebar}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
          </SidebarHeader>

          <SidebarContent>
            <SidebarGroup>
              <SidebarGroupLabel className="capitalize">
                {activeContent}
              </SidebarGroupLabel>
              <SidebarGroupContent>
                <SidebarMenu>
                  {navItems.map((item) => {
                    const Icon = item.icon;
                    return (
                      <SidebarMenuItem key={item.id}>
                        <SidebarMenuButton
                          onClick={() => {
                            console.log("Navigate to:", item.id);
                          }}
                        >
                          <Icon className="h-4 w-4" />
                          <span>{item.label}</span>
                        </SidebarMenuButton>
                      </SidebarMenuItem>
                    );
                  })}
                </SidebarMenu>
              </SidebarGroupContent>
            </SidebarGroup>
          </SidebarContent>
        </Sidebar>

        <SidebarInset>
          <div className="flex-1 overflow-auto bg-muted/30 h-full">
            <div className="container mx-auto p-6">
              <div className="space-y-4">
                <div>
                  <h1 className="text-3xl font-bold capitalize">
                    {activeContent}
                  </h1>
                  <p className="text-muted-foreground">
                    Content for {activeContent} page goes here
                  </p>
                </div>

                {/* Placeholder content - replace with actual content components */}
                <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                  {[1, 2, 3, 4, 5, 6].map((i) => (
                    <div
                      key={i}
                      className="rounded-lg border bg-card p-6 shadow-sm"
                    >
                      <h3 className="font-semibold">Card {i}</h3>
                      <p className="text-sm text-muted-foreground mt-2">
                        This is placeholder content for the {activeContent}{" "}
                        section.
                      </p>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </SidebarInset>
      </SidebarProvider>
    </div>
  );
}
