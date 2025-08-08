"use client"

import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { ChevronDown } from 'lucide-react'
import { useSidebar } from "@/components/ui/sidebar"
import { cn } from "@/lib/utils"
import { GroupWorkspace } from "@/features/workspace/workspacetype"
import { WorkspaceSummary } from "@/types/workspace"

interface WorkspaceSwitcherProps {
  data: GroupWorkspace | undefined;
  isLoading: boolean;
}

const getInitials = (name: string) => {
  return name
    .split(' ')
    .map(word => word.charAt(0))
    .join('')
    .toUpperCase()
    .slice(0, 2)
}

export function WorkspaceSwitcher({ data, isLoading }: WorkspaceSwitcherProps) {
  const { state } = useSidebar()
  const isCollapsed = state === "collapsed"

  const currentWorkspace = data?.currentWorkspace;

  if (isLoading || !currentWorkspace) {
    // Render a skeleton or placeholder while loading or if data is not available
    return (
        <div className={cn("h-12 flex items-center", isCollapsed ? "justify-center" : "px-2")}>
            <div className="h-8 w-8 bg-gray-200 rounded-lg animate-pulse"></div>
            {!isCollapsed && <div className="ml-2 h-4 w-24 bg-gray-200 rounded animate-pulse"></div>}
        </div>
    );
  }

  return (
    <Dialog>
      <DialogTrigger asChild>
        <Button
          variant="ghost"
          className={cn(
            "w-full hover:bg-sidebar-accent hover:text-sidebar-accent-foreground transition-all duration-200",
            isCollapsed
              ? "h-8 w-8 p-0 justify-start mx-0.75"
              : "h-12 justify-start px-0.75"
          )}
        >
          <div className="flex items-center gap-2 w-full">
            <Avatar className="h-8 w-8 shrink-0 rounded-lg">
              <AvatarFallback className="bg-sidebar-primary text-sidebar-primary-foreground text-sm rounded-lg">
                {getInitials(currentWorkspace.name)}
              </AvatarFallback>
            </Avatar>
            {!isCollapsed && (
              <>
                <div className="flex flex-col items-start min-w-0 flex-1">
                  <span className="text-sm font-medium truncate w-full text-left">{currentWorkspace.name}</span>
                  <span className="text-xs text-sidebar-foreground/70 truncate w-full text-left">Workspace</span>
                </div>
                <ChevronDown className="h-4 w-4 shrink-0 ml-auto" />
              </>
            )}
          </div>
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Switch Workspace</DialogTitle>
        </DialogHeader>
        <div className="flex flex-col space-y-2">
          {data?.otherWorkspaces.map((workspace: WorkspaceSummary) => (
            <Button key={workspace.id} variant="ghost" className="justify-start">
              {/* You would typically wrap this in a Link component from Next.js */}
              {workspace.name}
            </Button>
          ))}
        </div>
      </DialogContent>
    </Dialog>
  )
}