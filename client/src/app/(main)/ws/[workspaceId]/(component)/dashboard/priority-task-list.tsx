"use client"

import { useState, useMemo, useEffect } from "react"
import { useInView } from "react-intersection-observer"
import { useInfiniteTasks } from "@/features/task/task-hooks"
import { Button } from "@/components/ui/button"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { Settings, Maximize2, MoreHorizontal, User, Plus, ChevronDown, Loader2 } from "lucide-react"
import { Priority, mapPriorityToBadge } from "@/utils/priority-utils"
import { PriorityBadge } from "@/components/custom/priority-badge"
import { UserIconBar } from "@/components/custom/user-icon-bar"
import { useWorkspaceId } from "@/utils/current-layer-id"
import { formatDate } from "@/utils/format-date"

export function PriorityTaskList() {
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
    <div className="min-h-screen bg-background p-6 flex items-center justify-center">
      <div className="w-full max-w-5xl bg-card text-card-foreground rounded-xl shadow-2xl border overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b">
          <div className="flex items-center gap-4">
            <h1 className="text-lg font-medium">Priority</h1>
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
        </div>

        <div className="p-0">
          <div className="border-b">
            <div className="flex items-center px-4 py-3 text-xs text-muted-foreground font-medium">
              <div className="w-6 flex justify-center"></div>
              <div className="flex-1 pl-4">Name</div>
              <div className="w-40 flex justify-center">Assignee</div>
              <div className="w-24 flex justify-center">Due date</div>
              <div className="w-32 flex justify-center">Priority</div>
              <div className="w-8 flex justify-center"></div>
            </div>
          </div>

          {status === "pending" ? (
            <div className="flex justify-center items-center py-8 h-[60vh]">
              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
            </div>
          ) : status === "error" ? (
            <div className="text-center py-8 text-destructive h-[60vh]">Error: {error.message}</div>
          ) : tasks.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground h-[60vh]">
              No tasks found.
            </div>
          ) : (
            <div className="overflow-y-auto h-[60vh]">
              {tasks.map((task, index) => {
                const dueDate = formatDate(task.dueDate)
                const isOverdue = task.dueDate && new Date(task.dueDate) < new Date()
                const isLastElement = index === tasks.length - 1
                const itemRef = isLastElement ? ref : null

                return (
                  <div
                    ref={itemRef}
                    key={task.id}
                    className="group relative flex items-center px-4 py-3 border-b transition-colors duration-200 hover:bg-muted/50"
                  >
                    <div className="w-6 flex justify-center">
                      <div className="w-4 h-4 rounded-full border opacity-0 group-hover:opacity-100 transition-opacity"></div>
                    </div>

                    <div className="flex-1 pl-4 min-w-0">
                      <div className="font-medium truncate mb-1">{task.name}</div>
                    </div>

                    <div className="absolute left-1/2 transform -translate-x-1/2 flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                      <Button variant="ghost" size="sm" className="h-6 w-6 p-0 bg-secondary hover:bg-secondary/80">
                        <Plus className="h-3 w-3" />
                      </Button>
                      <Button variant="ghost" size="sm" className="h-6 w-6 p-0 bg-secondary hover:bg-secondary/80">
                        <svg className="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
                          />
                        </svg>
                      </Button>
                      <Button variant="ghost" size="sm" className="h-6 w-6 p-0 bg-secondary hover:bg-secondary/80">
                        <svg className="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"
                          />
                        </svg>
                      </Button>
                    </div>

                    <div className="w-40 flex justify-center">
                      <div className="group/cell rounded-sm border border-transparent hover:bg-accent hover:border-accent-foreground/30 px-3 py-2 transition-colors duration-200 min-h-[40px] flex items-center justify-center">
                        <div className="w-32 flex justify-center">
                          <UserIconBar users={task.assignees} maxIcons={4} />
                        </div>
                      </div>
                    </div>

                    <div className="w-26 text-center">
                      <div className="group/cell rounded-sm border border-transparent hover:bg-accent hover:border-accent-foreground/30 px-3 py-2 transition-colors duration-200 min-h-[40px] flex items-center justify-center">
                        {dueDate && (
                          <span className={`text-xs ${isOverdue ? "text-destructive" : "text-muted-foreground"}`}>{dueDate}</span>
                        )}
                      </div>
                    </div>

                    <div className="w-32 flex justify-center">
                      <div className="group/cell rounded-sm border border-transparent hover:bg-accent hover:border-accent-foreground/30 px-3 py-2 transition-colors duration-200 min-h-[40px] flex items-center justify-center">
                        <div className="w-20 flex justify-center">
                          <PriorityBadge priority={task.priority} size="sm" />
                        </div>
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
            </div>
          )}
        </div>
      </div>
    </div>
  )
}