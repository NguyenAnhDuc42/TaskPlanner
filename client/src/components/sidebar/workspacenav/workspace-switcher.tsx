"use client"

import * as React from "react"
import { ChevronsUpDown, Plus } from "lucide-react"
import { SidebarMenu, SidebarMenuItem, useSidebar } from "@/components/ui/sidebar"
import { useSidebarWorkspaces } from "@/features/workspace/workspace-hooks"
import { usePathname, useRouter } from "next/navigation"
import { useWorkspaceStore } from "@/utils/workspace-store"

import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { AddWorkspaceButton } from "./add-workspace-form"
import Image from "next/image"
import { Skeleton } from "@/components/ui/skeleton"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

export function WorkspaceSwitcher() {
  const router = useRouter()
  const { data, isLoading } = useSidebarWorkspaces()
  const { selectedWorkspaceId, setSelectedWorkspaceId } = useWorkspaceStore()
  const { state } = useSidebar()
  const workspaces = data?.workspaces || []
  const [isDialogOpen, setIsDialogOpen] = React.useState(false)
  const [showSkeleton, setShowSkeleton] = React.useState(true)
  const pathname = usePathname()

  const selectedWorkspace = selectedWorkspaceId ? workspaces.find((w) => w.id === selectedWorkspaceId) : null

  React.useEffect(() => {
    if (!isLoading) {
      const timer = setTimeout(() => setShowSkeleton(false), 300)
      return () => clearTimeout(timer)
    }
  }, [isLoading])

  React.useEffect(() => {
    if (!isLoading && workspaces.length > 0) {
      const isWorkspaceRoute = pathname.startsWith("/ws")
      if ((isWorkspaceRoute && !selectedWorkspaceId) || (pathname === "/" && !selectedWorkspaceId)) {
        setSelectedWorkspaceId(workspaces[0].id)
        router.replace(`/ws/${workspaces[0].id}`)
      }
    }
  }, [isLoading, workspaces, selectedWorkspaceId, setSelectedWorkspaceId, router, pathname])

  const handleWorkspaceSelect = (workspaceId: string) => {
    setSelectedWorkspaceId(workspaceId)
    router.push(`/ws/${workspaceId}`)
    setIsDialogOpen(false)
  }

  return (
    <SidebarMenu className={cn("rounded-none m-0 p-0 gap-0", state === "collapsed" && "px-2")}>
      <SidebarMenuItem className="rounded-none m-0 p-0">
        <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
          <DialogTrigger asChild>
            {showSkeleton ? (
              <div className={cn("flex items-center w-full", state === "collapsed" ? "justify-center p-2" : "px-3 py-2")}>
                <Skeleton className={cn(state === "collapsed" ? "size-6 rounded-lg" : "size-10 rounded-none")} />
                {state !== "collapsed" && (
                  <>
                    <div className="ml-3 flex-1">
                      <Skeleton className={cn("h-4 w-3/4", "rounded-none")} />
                    </div>
                    <Skeleton className={cn("size-4", "rounded-none")} />
                  </>
                )}
              </div>
            ) : (
              <Button variant="ghost" 
              className={cn("data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground",
                            "hover:bg-sidebar-accent/80 transition-all duration-200 m-0",
                            state === "collapsed"  ? "rounded-lg size-8 p-0 my-3 justify-center"
                                                   : "w-full justify-start h-14 px-3 rounded-none",)}>

                <div className={cn("bg-sidebar-primary text-sidebar-primary-foreground flex items-center justify-center rounded-lg",
                    state === "collapsed" ? "size-6 rounded-sm" : "aspect-square size-10 rounded-md",)}>

                  {selectedWorkspace?.icon ? (
                    <Image
                      src={selectedWorkspace.icon || "/placeholder.svg"}
                      alt={selectedWorkspace.name}
                      width={state === "collapsed" ? 12 : 16}
                      height={state === "collapsed" ? 12 : 16}
                      className={state === "collapsed" ? "size-3" : "size-4"}
                    />
                  ) : workspaces.length > 0 ? (
                    <ChevronsUpDown className={state === "collapsed" ? "size-3" : "size-4"} />
                  ) : (
                    <Plus className={state === "collapsed" ? "size-3" : "size-4"} />
                  )}
                </div>
                {state !== "collapsed" && (
                  <>
                    <div className="grid flex-1 text-left leading-tight ml-3">
                      <span className="truncate font-medium text-base">
                        {selectedWorkspace?.name || (workspaces.length > 0 ? "Select workspace" : "Add workspace")}
                      </span>
                    </div>
                    <ChevronsUpDown className="ml-auto" />
                  </>
                )}
              </Button>
            )}
          </DialogTrigger>
          <DialogContent className={cn("max-w-md", "rounded-none")} onInteractOutside={(e) => e.preventDefault()}>
            <DialogHeader>
              <DialogTitle className="text-left text-lg">Workspaces</DialogTitle>
            </DialogHeader>

            {isLoading ? (
              <div className="space-y-3 py-2">
                {[...Array(3)].map((_, i) => (
                  <div key={i} className="flex items-center gap-3 p-3">
                    <Skeleton className={cn("size-8", "rounded-none")} />
                    <Skeleton className={cn("h-4 flex-1", "rounded-none")} />
                  </div>
                ))}
              </div>
            ) : (
              <div className="py-2">
                <div className="max-h-96 overflow-y-auto space-y-1">
                  {workspaces.length === 0 ? (
                    <p className="text-muted-foreground text-center py-4">No workspaces found</p>
                  ) : (
                    workspaces.map((workspace) => (
                      <Button
                        key={workspace.id}
                        variant="ghost"
                        className={cn(
                          "w-full justify-start h-14 px-3 hover:bg-accent rounded-none",
                          workspace.id === selectedWorkspaceId ? "bg-accent" : "",
                        )}
                        onClick={() => handleWorkspaceSelect(workspace.id)}
                      >
                        {workspace.icon ? (
                          <Image
                            src={workspace.icon || "/placeholder.svg"}
                            alt={workspace.name}
                            width={32}
                            height={32}
                            className="size-8"
                          />
                        ) : (
                          <div className="bg-muted flex size-8 items-center justify-center font-medium rounded-none">
                            {workspace.name.charAt(0)}
                          </div>
                        )}
                        <span className="font-medium truncate text-left text-base ml-3">{workspace.name}</span>
                      </Button>
                    ))
                  )}
                </div>

                <div className="mt-4 flex justify-center">
                  <AddWorkspaceButton
                    afterAdd={() => {
                      setIsDialogOpen(true)
                    }}
                  />
                </div>
              </div>
            )}
          </DialogContent>
        </Dialog>
      </SidebarMenuItem>
    </SidebarMenu>
  )
}
