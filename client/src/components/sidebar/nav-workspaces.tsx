"use client"

import type * as React from "react"
import { MoreHorizontal, Plus } from "lucide-react"

import { useSidebarWorkspaces } from "@/features/workspace/workspace-hooks"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSkeleton,
} from "@/components/ui/sidebar"

export function NavWorkspaces({ ...props }: React.ComponentPropsWithoutRef<typeof SidebarGroup>) {
  const { data, isLoading } = useSidebarWorkspaces()

  return (
    <SidebarGroup {...props}>
      <SidebarGroupLabel className="flex items-center">
        <span>Workspaces</span>
        <Button variant="ghost" size="icon" className="ml-auto size-5">
          <Plus className="size-4" />
        </Button>
      </SidebarGroupLabel>
      <SidebarGroupContent>
        <SidebarMenu>
          {isLoading && Array.from({ length: 3 }).map((_, i) => <SidebarMenuSkeleton key={i} showIcon />)}
          {data?.workspaces.map((ws) => (
            <SidebarMenuItem key={ws.id} className="group/item">
              <SidebarMenuButton size="sm" className="w-full pr-1">
                <span className="text-lg">{ws.icon}</span>
                <span className="flex-1 truncate">{ws.name}</span>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon" className="size-6 opacity-0 group-hover/item:opacity-100">
                      <MoreHorizontal className="size-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent>
                    <DropdownMenuItem>Settings</DropdownMenuItem>
                    <DropdownMenuItem>Invite members</DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </SidebarMenuButton>
            </SidebarMenuItem>
          ))}
        </SidebarMenu>
      </SidebarGroupContent>
    </SidebarGroup>
  )
}
