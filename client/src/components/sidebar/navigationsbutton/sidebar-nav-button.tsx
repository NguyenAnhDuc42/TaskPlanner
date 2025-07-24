"use client"

import * as React from "react"
import Link from "next/link"
import { SidebarMenuItem, SidebarMenuButton, useSidebar } from "@/components/ui/sidebar"
import { cn } from "@/lib/utils"

interface SidebarNavButtonProps {
  href: string
  icon: React.ElementType
  label: string
  className?: string
  disabled?: boolean
}

export function SidebarNavButton({ href, icon: Icon, label, className, disabled }: SidebarNavButtonProps) {
  const { state } = useSidebar()

  return (
    <SidebarMenuItem className={cn("m-0 p-0", className)}>
      <SidebarMenuButton  asChild
        tooltip={label}
        className={cn(
          "transition-all duration-200 m-0 border border-transparent hover:bg-sidebar-accent hover:border-sidebar-border",
          state === "collapsed" ? "rounded-lg size-8 p-0 justify-center my-1" : "rounded-none h-auto p-4",
        )}
      >
        <Link href={href} aria-disabled={disabled}>
          <Icon className={state === "collapsed" ? "h-5 w-5" : "h-4 w-4"} />
          {state !== "collapsed" && <span>{label}</span>}
        </Link>
      </SidebarMenuButton>
    </SidebarMenuItem>
  )
}

