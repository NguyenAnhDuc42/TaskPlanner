"use client"

import { useMemo, useEffect } from "react"
import { useInView } from "react-intersection-observer"
import { useInfiniteTasks } from "@/features/task/task-hooks"
import { Button } from "@/components/ui/button"
import { MoreHorizontal, Loader2, Plus } from "lucide-react"
import { PriorityBadge } from "@/components/custom/priority-badge"
import { UserIconBar } from "@/components/custom/user-icon-bar"
import { useWorkspaceId } from "@/utils/current-layer-id"
import { formatDate } from "@/utils/format-date"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"

import { cn } from "@/lib/utils"; // Add this import

export function DueSoonTaskList({ className }: { className?: string }) {
  const { ref, inView } = useInView()
  const workspaceId = useWorkspaceId()

  const query = useMemo(() => {
    const now = new Date()
    const sevenDaysFromNow = new Date(new Date().setDate(now.getDate() + 7))

    return {
      workspaceId,
      DueDateAfter: new Date().toISOString(),
      DueDateBefore: sevenDaysFromNow.toISOString(),
      SortBy: "DueDate",
      Direction: "Asc",
    }
  }, [workspaceId])

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
    <Card className={cn("w-full max-w-2xl shadow-lg", className)}>
      <CardHeader className="flex flex-row items-center justify-between p-4 border-b">
        <CardTitle className="text-lg font-medium">Tasks Due Soon</CardTitle>
      </CardHeader>

      <CardContent className="p-0">
        <div className="border-b">
          <div className="flex items-center px-4 py-3 text-xs text-muted-foreground font-medium">
            <div className="flex-1 pl-4">Name</div>
            <div className="w-40 flex justify-center">Assignee</div>
            <div className="w-24 flex justify-center">Due date</div>
            <div className="w-32 flex justify-center">Priority</div>
            <div className="w-8 flex justify-center"></div>
          </div>
        </div>

        {status === "pending" ? (
          <div className="flex justify-center items-center py-8 h-[40vh]">
            <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
          </div>
        ) : status === "error" ? (
          <div className="text-center py-8 text-destructive h-[40vh]">Error: {error.message}</div>
        ) : tasks.length === 0 ? (
          <div className="text-center py-8 text-muted-foreground h-[40vh]">
            No tasks due in the next 7 days.
          </div>
        ) : (
          <div className="overflow-y-auto h-[40vh]">
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
      </CardContent>
    </Card>
  )
}
