"use client"

import { type LucideIcon } from 'lucide-react'
import Link from 'next/link'
import { SidebarMenuButton, SidebarMenuItem } from "@/components/ui/sidebar"
import { cn } from "@/lib/utils"

interface SidebarNavItemProps {
  title: string
  icon: LucideIcon
  href?: string
  isActive?: boolean
  onClick?: () => void
}

export function SidebarNavItem({ title, icon: Icon, href = "#", isActive, onClick }: SidebarNavItemProps) {
  return (
    <SidebarMenuItem>
      <SidebarMenuButton 
        asChild 
        isActive={isActive}
        tooltip={title}
        className={cn(
          "h-8 w-full justify-start transition-all duration-200",
          "group-data-[collapsible=icon]:!p-2 group-data-[collapsible=icon]:w-[var(--sidebar-width-icon)]" 
        )}
        onClick={onClick}
      >
        <Link href={href} className="flex items-center gap-2">
          <Icon className="h-4 w-4 shrink-0" />
          <span className="truncate group-data-[collapsible=icon]:hidden">{title}</span>
        </Link>
      </SidebarMenuButton>
    </SidebarMenuItem>
  )
}
