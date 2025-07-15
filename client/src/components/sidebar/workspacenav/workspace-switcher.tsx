"use client";

import * as React from "react";
import { ChevronsUpDown, Plus, Loader2 } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  useSidebar,
} from "@/components/ui/sidebar";
import {
  useSidebarWorkspaces,
} from "@/features/workspace/workspace-hooks";
import { useRouter } from "next/navigation";
import { WorkspaceList } from "./workspace-list";
import { useWorkspaceStore } from "@/utils/workspace-store";


export function WorkspaceSwitcher() {
  const router = useRouter();
  const { isMobile } = useSidebar();
  const { data, isLoading } = useSidebarWorkspaces();
  const { selectedWorkspaceId, setSelectedWorkspaceId } = useWorkspaceStore();
  const workspaces = data?.workspaces || [];

  const selectedWorkspace = workspaces.find(w => w.id === selectedWorkspaceId);

  const handleWorkspaceSelect = (workspaceId: string) => {
    setSelectedWorkspaceId(workspaceId);
    router.push(`/workspace/${workspaceId}`);
  };

  return (
    <SidebarMenu>
      <SidebarMenuItem>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <SidebarMenuButton
              size="lg"
              className="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground"
            >
              <div className="bg-sidebar-primary text-sidebar-primary-foreground flex aspect-square size-8 items-center justify-center rounded-lg">
                {selectedWorkspace?.icon ? (
                  <img src={selectedWorkspace.icon} alt={selectedWorkspace.name} className="size-4" />
                ) : workspaces.length > 0 ? (
                  <ChevronsUpDown className="size-4" />
                ) : (
                  <Plus className="size-4" />
                )}
              </div>
              <div className="grid flex-1 text-left text-sm leading-tight">
                <span className="truncate font-medium">
                  {selectedWorkspace?.name || (workspaces.length > 0 
                    ? "Select workspace" 
                    : "Add workspace")}
                </span>
              </div>
              <ChevronsUpDown className="ml-auto" />
            </SidebarMenuButton>
          </DropdownMenuTrigger>
          <DropdownMenuContent
            className="w-(--radix-dropdown-menu-trigger-width) min-w-56 rounded-lg"
            align="start"
            side={isMobile ? "bottom" : "right"}
            sideOffset={4}
          >
            <DropdownMenuLabel className="text-muted-foreground text-xs">
              Workspaces
            </DropdownMenuLabel>
            {isLoading ? (
              <div className="flex justify-center items-center py-4">
                <Loader2 className="animate-spin h-5 w-5 text-muted-foreground" />
              </div>
            ) : (
              <WorkspaceList 
                workspaces={workspaces} 
                onSelect={handleWorkspaceSelect} 
              />
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      </SidebarMenuItem>
    </SidebarMenu>
  );
}