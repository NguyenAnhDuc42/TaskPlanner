"use client"

import { UserDetail } from "@/types/user"
import { 
  DropdownMenu, 
  DropdownMenuContent, 
  DropdownMenuItem, 
  DropdownMenuLabel, 
  DropdownMenuSeparator, 
  DropdownMenuTrigger 
} from "@/components/ui/dropdown-menu"
import { Button } from "@/components/ui/button"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { ChevronDown } from 'lucide-react'
import { useSidebar } from "@/components/ui/sidebar"
import { cn } from "@/lib/utils"

interface SidebarUserMenuProps {
  user: UserDetail
  onProfileSettings?: () => void
  onNotifications?: () => void
  onBilling?: () => void
  onSignOut?: () => void
}

export function SidebarUserMenu({ 
  user, 
  onProfileSettings, 
  onNotifications, 
  onBilling, 
  onSignOut 
}: SidebarUserMenuProps) {
  const { state } = useSidebar()
  const isCollapsed = state === "collapsed"

  const getInitials = (name: string) => {
    return name
      .split(' ')
      .map(word => word.charAt(0))
      .join('')
      .toUpperCase()
      .slice(0, 2)
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          className={cn(
            "w-full hover:bg-sidebar-accent hover:text-sidebar-accent-foreground transition-all duration-200",
            isCollapsed
              ? "h-8 w-8 p-0 justify-start mx-0.75" // Collapsed: button shrinks to avatar size, no padding, centers content
              : "h-12 justify-start px-0.75" // Expanded: full height, NO left padding for max left shift, left align content
          )}
        >
          <div className="flex items-center gap-2 min-w-0">
            <Avatar className="h-8 w-8 shrink-0 rounded-lg">
              <AvatarFallback className="bg-sidebar-primary text-sidebar-primary-foreground text-sm rounded-lg">
                {getInitials(user.name)}
              </AvatarFallback>
            </Avatar>
            {!isCollapsed && (
              <>
                <div className="flex flex-col items-start min-w-0 flex-1">
                  <span className="text-sm font-medium truncate w-full">{user.name}</span>
                  <span className="text-xs text-sidebar-foreground/70 truncate w-full">{user.email}</span>
                </div>
                <ChevronDown className="h-4 w-4 shrink-0 ml-auto" />
              </>
            )}
          </div>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuLabel>
          <div className="flex flex-col space-y-1">
            <p className="text-sm font-medium">{user.name}</p>
            <p className="text-xs text-muted-foreground">{user.email}</p>
          </div>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={onProfileSettings}>
          Profile Settings
        </DropdownMenuItem>
        <DropdownMenuItem onClick={onNotifications}>
          Notifications
        </DropdownMenuItem>
        <DropdownMenuItem onClick={onBilling}>
          Billing
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={onSignOut} className="text-red-600">
          Sign Out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
