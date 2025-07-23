"use client"

import { useLogout, useUser } from "@/features/auth/hooks"
import { BadgeCheck, Bell, ChevronsUpDown, CreditCard, LogOut, Sparkles } from "lucide-react"

import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { SidebarMenu, SidebarMenuButton, SidebarMenuItem, useSidebar } from "@/components/ui/sidebar"
import { cn } from "@/lib/utils"

const getInitials = (name = "") => {
  const names = name.split(" ").filter(Boolean)
  if (names.length === 0) return "U"
  if (names.length === 1) return names[0].slice(0, 2).toUpperCase()
  return (names[0][0] + names[names.length - 1][0]).toUpperCase()
}

export function NavUser() {
  const { data: user, isLoading } = useUser()
  const logoutMutation = useLogout()
  const { isMobile, state } = useSidebar()

  if (isLoading) {
    return (
      <SidebarMenu className={cn("rounded-none m-0 p-0 gap-0", state === "collapsed" && "px-2")}>
        <SidebarMenuItem className="rounded-none m-0 p-0">
          <SidebarMenuButton
            size="lg"
            className={cn(
              "pointer-events-none transition-all duration-200 m-0",
              state === "collapsed" ? "rounded-lg size-8 p-0 justify-center" : "rounded-none",
            )}
          >
            <div
              className={cn(
                "flex items-center justify-center bg-sidebar-primary/40 animate-pulse",
                state === "collapsed" ? "size-4 rounded-sm" : "size-8 rounded-none",
              )}
            />
            {state !== "collapsed" && (
              <div className="flex-1 space-y-1.5">
                <div className="w-24 h-4 rounded-none bg-sidebar-primary/40 animate-pulse" />
                <div className="w-32 h-3 rounded-none bg-sidebar-primary/40 animate-pulse" />
              </div>
            )}
          </SidebarMenuButton>
        </SidebarMenuItem>
      </SidebarMenu>
    )
  }

  if (!user) {
    return null
  }

  return (
    <SidebarMenu className={cn("rounded-none m-0 p-0 gap-0", state === "collapsed" && "px-2")}>
      <SidebarMenuItem className="rounded-none m-0 p-0">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <SidebarMenuButton size="lg"className={cn("data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground",
                "hover:bg-sidebar-accent/80 transition-all duration-200 m-0 ",
                state === "collapsed" ? "rounded-lg size-8 my-3 justify-center" : "rounded-none",)}>

              <div className={cn("flex items-center justify-center bg-sidebar-primary text-sidebar-primary-foreground ",
                  state === "collapsed" ? "size-6 rounded-sm" : "size-8 rounded-md",)}>
                    
                <Avatar className={cn(state === "collapsed" ? "h-6 w-6 rounded-sm" : "h-8 w-8 rounded-md")}>
                  <AvatarImage src={user.avatar ?? ""} alt={user.name ?? ""} />
                  <AvatarFallback className={cn(state === "collapsed" ? "rounded-sm text-xs" : "rounded-none")}>
                    {getInitials(user.name)}
                  </AvatarFallback>
                </Avatar>
              </div>
              {state !== "collapsed" && (
                <>
                  <div className="grid flex-1 text-left text-sm leading-tight">
                    <span className="truncate font-medium">{user.name}</span>
                    <span className="truncate text-xs">{user.email}</span>
                  </div>
                  <ChevronsUpDown className="ml-auto size-4" />
                </>
              )}
            </SidebarMenuButton>
          </DropdownMenuTrigger>
          <DropdownMenuContent
            className={cn("w-[--radix-dropdown-menu-trigger-width] min-w-56", "rounded-none")}
            side={isMobile ? "bottom" : "right"}
            align="end"
            sideOffset={4}
          >
            <DropdownMenuLabel className="p-0 font-normal">
              <div className="flex items-center gap-2 px-1 py-1.5 text-left text-sm">
                <Avatar className={cn("h-8 w-8", "rounded-none")}>
                  <AvatarImage src={user.avatar ?? ""} alt={user.name ?? ""} />
                  <AvatarFallback className="rounded-none">{getInitials(user.name)}</AvatarFallback>
                </Avatar>
                <div className="grid flex-1 text-left text-sm leading-tight">
                  <span className="truncate font-medium">{user.name}</span>
                  <span className="truncate text-xs">{user.email}</span>
                </div>
              </div>
            </DropdownMenuLabel>
            <DropdownMenuSeparator />
            <DropdownMenuGroup>
              <DropdownMenuItem>
                <Sparkles />
                Upgrade to Pro
              </DropdownMenuItem>
            </DropdownMenuGroup>
            <DropdownMenuSeparator />
            <DropdownMenuGroup>
              <DropdownMenuItem>
                <BadgeCheck />
                Account
              </DropdownMenuItem>
              <DropdownMenuItem>
                <CreditCard />
                Billing
              </DropdownMenuItem>
              <DropdownMenuItem>
                <Bell />
                Notifications
              </DropdownMenuItem>
            </DropdownMenuGroup>
            <DropdownMenuSeparator />
            <DropdownMenuItem onSelect={() => logoutMutation.mutate()} disabled={logoutMutation.isPending}>
              <LogOut />
              Log out
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </SidebarMenuItem>
    </SidebarMenu>
  )
}
