"use client"

import { useSidebar } from "@/components/ui/sidebar"
import { cn } from "@/lib/utils"
import { 
  Dialog, 
  DialogContent, 
  DialogDescription, 
  DialogHeader, 
  DialogTitle, 
  DialogTrigger,
  DialogFooter,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"

interface SidebarHeaderBrandProps {
  name: string
  shortName: string
}

export function SidebarHeaderBrand({ name, shortName }: SidebarHeaderBrandProps) {
  const { state } = useSidebar()
  const isCollapsed = state === "collapsed"

  return (
    <Dialog>
      <DialogTrigger asChild>
        <Button
          variant="ghost"
          className={cn(
            "w-full hover:bg-sidebar-accent hover:text-sidebar-accent-foreground transition-all duration-200",
            isCollapsed
              ? "h-8 w-8 p-0 justify-start mx-0.75" // Collapsed: button shrinks, no padding, left-aligned with 3px padding
              : "h-12 justify-start px-[3px]" // Expanded: full height, left-aligned with 3px padding
          )}
        >
          <div className="flex items-center gap-2 min-w-0">
            <div className="flex items-center justify-center h-8 w-8 bg-sidebar-primary text-sidebar-primary-foreground rounded-lg shrink-0">
              <span className="text-sm font-semibold">{shortName}</span>
            </div>
            {!isCollapsed && (
              <div className="flex flex-col min-w-0">
                <span className="text-sm font-semibold truncate">{name}</span>
              </div>
            )}
          </div>
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Workspace Settings</DialogTitle>
          <DialogDescription>
            Manage your workspace details and settings here.
          </DialogDescription>
        </DialogHeader>
        <div className="grid gap-4 py-4">
          <p>This is a placeholder for your workspace settings content.</p>
          <p>You can add forms, options, or other information here.</p>
        </div>
        <DialogFooter>
          <Button type="button">Save changes</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
