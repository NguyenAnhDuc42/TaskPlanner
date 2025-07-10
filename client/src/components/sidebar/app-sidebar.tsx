"use client"

import type * as React from "react"
import { BookOpen, Bot, Command, Frame, LifeBuoy, Map, PieChart, Send, Settings2, SquareTerminal } from "lucide-react"

import { NavMain } from "@/components/sidebar/nav-main"
import { NavProjects } from "@/components/sidebar/nav-projects"
import { NavSecondary } from "@/components/sidebar/nav-secondary"
import { NavUser } from "@/components/sidebar/nav-user"
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/components/ui/sidebar"

const data = {
  user: {
    name: "Sarah Chen",
    email: "sarah@designteam.com",
    avatar: "/avatars/sarah.jpg",
  },
  navMain: [
    {
      title: "Home",
      url: "#",
      icon: SquareTerminal,
      isActive: true,
    },
    {
      title: "My Work",
      url: "#",
      icon: Bot,
      items: [
        {
          title: "My Tasks (12)",
          url: "#",
        },
        {
          title: "Assigned to me (8)",
          url: "#",
        },
        {
          title: "Following (24)",
          url: "#",
        },
        {
          title: "Created by me (15)",
          url: "#",
        },
      ],
    },
    {
      title: "Inbox",
      url: "#",
      icon: BookOpen,
      items: [
        {
          title: "Activity (3)",
          url: "#",
        },
        {
          title: "Messages (1)",
          url: "#",
        },
        {
          title: "Mentions",
          url: "#",
        },
      ],
    },
    {
      title: "Goals",
      url: "#",
      icon: Settings2,
      items: [
        {
          title: "Q1 Objectives",
          url: "#",
        },
        {
          title: "Team Goals",
          url: "#",
        },
        {
          title: "Personal Goals",
          url: "#",
        },
      ],
    },
  ],
  navSecondary: [
    {
      title: "Portfolios",
      url: "#",
      icon: LifeBuoy,
    },
    {
      title: "Reporting",
      url: "#",
      icon: Send,
    },
  ],
  projects: [
    {
      name: "🎨 Design System 2.0",
      url: "#",
      icon: Frame,
    },
    {
      name: "📱 Mobile App Redesign",
      url: "#",
      icon: PieChart,
    },
    {
      name: "🚀 Product Launch",
      url: "#",
      icon: Map,
    },
    {
      name: "📊 Analytics Dashboard",
      url: "#",
      icon: BookOpen,
    },
    {
      name: "🔧 API Integration",
      url: "#",
      icon: Bot,
    },
  ],
}

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  return (
    <Sidebar collapsible="icon" className="[&>[data-sidebar=sidebar]]:overflow-hidden" {...props}>
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton size="lg" asChild>
              <a href="#">
                <div className="flex aspect-square size-8 items-center justify-center rounded-lg bg-sidebar-primary text-sidebar-primary-foreground">
                  <Command className="size-4" />
                </div>
                <div className="grid flex-1 text-left text-sm leading-tight">
                  <span className="truncate font-semibold">Design Team</span>
                  <span className="truncate text-xs">Premium</span>
                </div>
              </a>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>
      <SidebarContent className="overflow-y-auto overflow-x-hidden custom-scrollbar">
        <NavMain items={data.navMain} />
        <NavProjects projects={data.projects} />
        <NavSecondary items={data.navSecondary} className="mt-auto" />
      </SidebarContent>
      <SidebarFooter>
        <NavUser/>
      </SidebarFooter>
    </Sidebar>
  )
}
