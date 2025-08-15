"use client"

import { useState, useMemo, useEffect } from "react"
import { useParams } from "next/navigation"
import { useInView } from "react-intersection-observer"
import { useInfiniteTasks } from "@/features/task/task-hooks"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Settings, Maximize2, MoreHorizontal, User, Loader2 } from "lucide-react"
import { Priority, mapPriorityToBadge } from "@/utils/priority-utils"
import { formatDate } from "@/utils/format-date"



export function PriorityTaskList() {
  const params = useParams()
  const { ref, inView } = useInView()

  const [selectedPriority, setSelectedPriority] = useState<Priority>(Priority.Urgent)
  const [showAssignedToMe, setShowAssignedToMe] = useState(false)

  const workspaceId = params.workspaceId as string

  const query = useMemo(() => {
    const baseQuery: { workspaceId: string; priority: Priority; assignedToMe?: boolean } = {
      workspaceId,
      priority: selectedPriority,
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

  const tasks = useMemo(() => data?.pages.flatMap((page) => page.data.tasks) ?? [], [data])

  return (
    <div className="min-h-screen bg-gray-950 p-6 flex items-center justify-center">
      <div className="w-full max-w-4xl bg-gray-900 text-white rounded-xl shadow-2xl border border-gray-700 overflow-hidden">
        <div className="flex items-center justify-between p-4 border-b border-gray-700">
          <h1 className="text-lg font-medium">Priority</h1>
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

        <Tabs value={selectedPriority} onValueChange={(value) => setSelectedPriority(value as Priority)}>
          <div className="border-b border-gray-700">
            <TabsList className="bg-transparent h-auto p-0 w-full justify-start">
              {Object.values(Priority).map((priority) => {
                const { priorityName } = mapPriorityToBadge(priority)
                return (
                  <TabsTrigger
                    key={priority}
                    value={priority}
                    className="bg-transparent text-gray-400 hover:text-white data-[state=active]:text-white data-[state=active]:bg-transparent data-[state=active]:border-b-2 data-[state=active]:border-blue-500 rounded-none px-4 py-3"
                  >
                    {priorityName}
                  </TabsTrigger>
                )
              })}
            </TabsList>
          </div>

          {Object.values(Priority).map((priority) => (
            <TabsContent key={priority} value={priority} className="mt-0">
              <div className="p-4">
                {status === "pending" ? (
                  <div className="flex justify-center items-center py-8">
                    <Loader2 className="h-8 w-8 animate-spin text-gray-500" />
                  </div>
                ) : status === "error" ? (
                  <div className="text-center py-8 text-red-500">Error: {error.message}</div>
                ) : tasks.length === 0 ? (
                  <div className="text-center py-8 text-gray-500">
                    {showAssignedToMe
                      ? `No ${priority.toLowerCase()} priority tasks assigned to you`
                      : `No ${priority.toLowerCase()} priority tasks`}
                  </div>
                ) : (
                  <div className="space-y-1">
                    {tasks.map((task, index) => {
                      const { badgeClasses, priorityName } = mapPriorityToBadge(task.priority)
                      const dueDate = formatDate(task.dueDate)
                      const isOverdue = task.dueDate && new Date(task.dueDate) < new Date()

                      const isLastElement = index === tasks.length - 1
                      const itemRef = isLastElement ? ref : null

                      return (
                        <div ref={itemRef} key={task.id} className="flex items-center py-2 px-2 hover:bg-gray-800 rounded group">
                          <div className="w-4 h-4 rounded-full border border-gray-600 mr-3 flex-shrink-0"></div>
                          <div className="flex-1 min-w-0">
                            <div className="flex items-center gap-2 mb-1">
                              <span className="text-white font-medium truncate">{task.name}</span>
                            </div>
                          </div>
                          <div className="flex items-center gap-4 text-xs">
                            <Badge variant="outline" className={badgeClasses}>
                              {priorityName}
                            </Badge>
                            {dueDate && <span className={isOverdue ? "text-red-400" : "text-gray-400"}>{dueDate}</span>}
                          </div>
                        </div>
                      )
                    })}
                    {isFetchingNextPage && (
                      <div className="flex justify-center items-center py-4">
                        <Loader2 className="h-6 w-6 animate-spin text-gray-500" />
                      </div>
                    )}
                  </div>
                )}
              </div>
            </TabsContent>
          ))}
        </Tabs>
      </div>
    </div>
  )
}