"use client"

import { useState, useMemo, useEffect } from "react"
import { useInView } from "react-intersection-observer"
import { useInfiniteTasks } from "@/features/task/task-hooks"
import { Button } from "@/components/ui/button"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { ScrollArea, ScrollBar } from "@/components/ui/scroll-area"
import { Settings, Maximize2, MoreHorizontal, User, Plus, ChevronDown, Loader2 } from "lucide-react"
import { Priority, mapPriorityToBadge } from "@/utils/priority-utils"
import { PriorityBadge } from "@/components/custom/priority-badge"
import { UserIconBar } from "@/components/custom/user-icon-bar"
import { useWorkspaceId } from "@/utils/current-layer-id"
import { formatDate } from "@/utils/format-date"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { cn } from "@/lib/utils";

export function PriorityTaskList({ className }: { className?: string }) {
  const { ref, inView } = useInView()

  const [selectedPriority, setSelectedPriority] = useState<Priority | "All">("All")
  const [showAssignedToMe, setShowAssignedToMe] = useState(false)

  const workspaceId = useWorkspaceId()

  const query = useMemo(() => {
    const baseQuery: { workspaceId: string; priority?: Priority; assignedToMe?: boolean } = {
      workspaceId,
    }
    if (selectedPriority !== "All") {
      baseQuery.priority = selectedPriority
    }
    if (showAssignedToMe) {
      baseQuery.assignedToMe = true
    }
    return baseQuery
  }, [workspaceId, selectedPriority, showAssignedToMe])

  const { data, error, fetchNextPage, hasNextPage, isFetchingNextPage, status } = useInfiniteTasks(query)

  useEffect(() => {
    if (inView && hasNextPage) {
      fetchNextPage()
    }
  }, [inView, hasNextPage, fetchNextPage])

  const tasks = useMemo(() => {
    const allTasks = data?.pages.flatMap((page) => page.data.tasks) ?? []
    const uniqueTasks = Array.from(new Map(allTasks.map((task) => [task.id, task])).values())
    return uniqueTasks
  }, [data])

  return (
    <Card className={cn("w-full max-w-6xl shadow-2xl overflow-hidden", className)}>
      <CardHeader className="flex flex-row items-center justify-between p-4 border-b">
        <div className="flex items-center gap-4">
          <CardTitle className="text-lg font-medium">Priority</CardTitle>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" className="flex items-center gap-2">
                {selectedPriority === "All" ? "Show All" : mapPriorityToBadge(selectedPriority).priorityName}
                <ChevronDown className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start">
              <DropdownMenuItem onClick={() => setSelectedPriority("All")}>
                Show All
              </DropdownMenuItem>
              {Object.values(Priority).map((priority) => {
                const { priorityName } = mapPriorityToBadge(priority)
                return (
                  <DropdownMenuItem
                    key={priority}
                    onClick={() => setSelectedPriority(priority)}
                  >
                    {priorityName}
                  </DropdownMenuItem>
                )
              })}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant={showAssignedToMe ? "default" : "ghost"}
            size="sm"
            onClick={() => setShowAssignedToMe(!showAssignedToMe)}
            className="text-xs"
          >
            <User className="h-3 w-3 mr-1" />
            Assigned to me
          </Button>
          <Button variant="ghost" size="sm">
            <Settings className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="sm">
            <Maximize2 className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="sm">
            <MoreHorizontal className="h-4 w-4" />
          </Button>
        </div>
      </CardHeader>

      <CardContent className="p-0">
        <div className="border-b sticky top-0 z-10">
          <div className="flex items-center px-4 py-3 text-xs text-muted-foreground font-medium">
            <div className="w-6 flex justify-center"></div>
            <div className="flex-1 pl-4">Name</div>
            <div className="w-40 flex justify-center">Assignee</div>
            <div className="w-24 flex justify-center">Due date</div>
            <div className="w-32 flex justify-center">Priority</div>
            <div className="w-8 flex justify-center"></div>
          </div>
        </div>

        <ScrollArea className="h-[600px] scrollbars">
          <div className="sb-reveal">
            {status === "pending" ? (
              <div className="flex items-center justify-center h-64">
                <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
              </div>
            ) : status === "error" ? (
              <div className="flex items-center justify-center text-center text-destructive h-64">
                Error: {error.message}
              </div>
            ) : tasks.length === 0 ? (
              <div className="flex flex-col justify-center items-center text-center text-muted-foreground h-40">
                No tasks found.
              </div>
            ) : (
              <>
                {tasks.map((task, index) => {
                  const dueDate = formatDate(task.dueDate)
                  const isOverdue = task.dueDate && new Date(task.dueDate) < new Date()
                  const isLastElement = index === tasks.length - 1
                  const itemRef = isLastElement ? ref : null

                  return (
                    <div
                      ref={itemRef}
                      key={task.id}
                      className="group flex items-center px-4 py-3 border-b transition-colors duration-200 hover:bg-muted/50 min-h-[60px]"
                    >
                      <div className="w-6 flex justify-center">
                        <div className="w-4 h-4 rounded-full border opacity-0 group-hover:opacity-100 transition-opacity"></div>
                      </div>

                      <div className="flex-1 pl-4 min-w-0 relative">
                        <div className="font-medium truncate mb-1">{task.name}</div>
                        <div className="absolute inset-0 flex items-center justify-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                          <Button variant="ghost" size="sm" className="h-6 w-6 p-0">
                            <Plus className="h-3 w-3" />
                          </Button>
                          <Button variant="ghost" size="sm" className="h-6 w-6 p-0">
                            <MoreHorizontal className="h-3 w-3" />
                          </Button>
                        </div>
                      </div>

                      <div className="w-40 flex justify-center">
                        <div className="group/cell rounded-sm border border-transparent hover:bg-accent hover:border-accent-foreground/30 px-3 py-2 transition-colors duration-200 min-h-[40px] flex items-center justify-center">
                          <UserIconBar users={task.assignees} maxIcons={4} />
                        </div>
                      </div>

                      <div className="w-24 text-center">
                        <div className="group/cell rounded-sm border border-transparent hover:bg-accent hover:border-accent-foreground/30 px-3 py-2 transition-colors duration-200 min-h-[40px] flex items-center justify-center">
                          {dueDate && (
                            <span className={`text-xs ${isOverdue ? "text-destructive" : "text-muted-foreground"}`}>
                              {dueDate}
                            </span>
                          )}
                        </div>
                      </div>

                      <div className="w-32 flex justify-center">
                        <div className="group/cell rounded-sm border border-transparent hover:bg-accent hover:border-accent-foreground/30 px-3 py-2 transition-colors duration-200 min-h-[40px] flex items-center justify-center">
                          <PriorityBadge priority={task.priority} size="sm" />
                        </div>
                      </div>

                      <div className="w-8 flex justify-center">
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-6 w-6 p-0 opacity-0 group-hover:opacity-100 transition-opacity"
                        >
                          <MoreHorizontal className="h-3 w-3" />
                        </Button>
                      </div>
                    </div>
                  )
                })}
                {isFetchingNextPage && (
                  <div className="flex justify-center items-center py-4">
                    <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
                  </div>
                )}
              </>
            )}
          </div>
          <ScrollBar orientation="horizontal" />
        </ScrollArea>
      </CardContent>
    </Card>
  )
}