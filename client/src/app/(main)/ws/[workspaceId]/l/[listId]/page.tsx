"use client"

import { use, useState } from "react"
import {
  ChevronDown,
  Filter,
  Search,
  Settings,
  CheckCircle,
  Calendar,
  Flag,
  MessageSquare,
  CircleDot,
  Circle,
} from "lucide-react"

import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import type { PlanTaskStatus } from "@/types/task"
import { useListTasks } from "@/features/list/list-hooks"
import { CreateTaskForm } from "@/components/task/create-task-form"
import Link from "next/link"


export default function Page( { params }: { params: Promise<{ workspaceId: string; listId: string }> }) {
  const resolvedParams = use(params);
  const listId = resolvedParams.listId as string | undefined
  const workspaceId = resolvedParams.workspaceId as string
  const { data: tasks, isLoading, isError } = useListTasks(listId)
  console.log(workspaceId)


  const [collapsedGroups, setCollapsedGroups] = useState<Record<PlanTaskStatus, boolean>>({
    ToDo: false,
    InProgress: false,
    InReview: false,
    Done: false,
  })

  // State for the new task form
  const [isCreateFormOpen, setIsCreateFormOpen] = useState(false)
  const [initialStatusForNewTask, setInitialStatusForNewTask] = useState<PlanTaskStatus>("ToDo")

  const handleAddTaskClick = (status: PlanTaskStatus) => {
    setInitialStatusForNewTask(status)
    setIsCreateFormOpen(true)
  }

  const toggleGroupCollapse = (status: PlanTaskStatus) => {
    setCollapsedGroups((prev) => ({
      ...prev,
      [status]: !prev[status],
    }))
  }

  const getStatusColor = (status: PlanTaskStatus) => {
    switch (status) {
      case "ToDo":
        return "bg-[#333333] text-gray-300 border border-gray-500" // Dark grey with border
      case "InProgress":
        return "bg-[#2563EB] text-blue-100" // Vibrant blue
      case "InReview":
        return "bg-yellow-600 text-yellow-100"
      case "Done":
        return "bg-green-600 text-green-100"
      default:
        return "bg-gray-600 text-gray-200"
    }
  }
  const getStatusIcon = (status: PlanTaskStatus | number) => {
    switch (status) {
      case "ToDo":
        return <Circle className="w-3 h-3 fill-current text-gray-400" />
      case "InProgress":
        return <CircleDot className="w-3 h-3 fill-current text-blue-300" /> 
      case "InReview":
        return <CircleDot className="w-3 h-3 fill-current text-yellow-400" />
      case "Done":
        return <CheckCircle className="w-3 h-3 fill-current text-green-400" />
      default:
        return <Circle className="w-3 h-3 fill-current text-gray-400" />
    }
  }

  const allStatuses: PlanTaskStatus[] = ["InProgress", "ToDo", "InReview", "Done"]

  if (isLoading) {
    return <div className="p-4 text-center text-gray-400">Loading tasks...</div>
  }

  if (isError || !tasks) {
    return <div className="p-4 text-center text-red-400">Error loading tasks. Please try again.</div>
  }

  return (
    <div className="min-h-screen flex flex-col bg-[#1a1a1a] text-gray-100">
      <header className="sticky top-0 z-10 flex items-center justify-between py-3 px-4 border-b border-gray-800 bg-[#1a1a1a] shrink-0">
        <div className="flex items-center space-x-2">
          {/* <Button variant="ghost" className="text-gray-300 hover:bg-gray-800 px-3 py-1 rounded-md">
            <span className="mr-2">Group: Status</span>
            <ChevronDown className="w-4 h-4" />
          </Button>
          <Button variant="ghost" className="text-gray-300 hover:bg-gray-800 px-3 py-1 rounded-md">
            <span className="mr-2">Subtasks</span>
            <ChevronDown className="w-4 h-4" />
          </Button>
          <Button variant="ghost" className="text-gray-300 hover:bg-gray-800 px-3 py-1 rounded-md">
            <span className="mr-2">Columns</span>
            <ChevronDown className="w-4 h-4" />
          </Button> */}
        </div>
        <div className="flex items-center space-x-2">
          <Button variant="ghost" size="icon" className="text-gray-300 hover:bg-gray-800">
            <Filter className="w-4 h-4" />
          </Button>
          <Button variant="ghost" size="icon" className="text-gray-300 hover:bg-gray-800">
            <Search className="w-4 h-4" />
          </Button>
          <Button variant="ghost" size="icon" className="text-gray-300 hover:bg-gray-800">
            <Settings className="w-4 h-4" />
          </Button>
          {/* Simplified Add Task button */}
          <Button className="bg-blue-600 hover:bg-blue-700 text-white" onClick={() => handleAddTaskClick("ToDo")}>
            Add Task
          </Button>
        </div>
      </header>
      {/* Main Content - now scrollable */}
      <main className="p-4 flex-1 overflow-y-auto">
        {allStatuses.map((status) => (
          <div key={status} className="mb-6">
            <div className="flex items-center justify-between py-2 px-2 rounded-t-md bg-[#282828]">
              {" "}
              {/* Lighter dark grey */}
              <div className="flex items-center space-x-2">
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => toggleGroupCollapse(status)}
                  className="text-gray-400 hover:bg-gray-700"
                >
                  <ChevronDown
                    className={`w-4 h-4 transition-transform ${collapsedGroups[status] ? "-rotate-90" : ""}`}
                  />
                </Button>
                <Badge className={`${getStatusColor(status)} px-3 py-1 rounded-full text-xs font-semibold`}>
                  {status.toUpperCase()}
                </Badge>
                <span className="text-gray-400 text-sm">{tasks.tasks[status]?.length || 0}</span>
              </div>
            </div>

            {!collapsedGroups[status] && (
              <div className="bg-[#242424] rounded-b-md overflow-hidden">
                <div className="grid grid-cols-[40px_1fr_140px_120px_100px_100px_40px] items-center py-2 px-2 text-xs font-medium text-gray-400 border-b border-gray-800">
                  <div className="col-span-1"></div> {/* For checkbox/play button */}
                  <div className="col-span-1">Name</div>
                  <div className="col-span-1">Due date</div>
                  <div className="col-span-1">Priority</div>
                  <div className="col-span-1">Status</div>
                  <div className="col-span-1">Comments</div>
                </div>
                {/* Task Rows */}
                {!tasks.tasks[status] || tasks.tasks[status]?.length === 0 ? (
                  <div className="text-center py-4 text-gray-500">No tasks in this status.</div>
                ) : (
                  tasks.tasks[status]?.map((task) => (
                    <div
                      key={task.id}
                      className="grid grid-cols-[40px_1fr_140px_120px_100px_100px_40px] items-center py-2 px-2 border-b border-gray-800 hover:bg-[#2e2e2e]" // Subtle hover
                    >
                      <div className="flex items-center space-x-2">
                        <Button variant="ghost" size="icon" className="w-6 h-6 text-gray-400 hover:bg-gray-700">
                          <Circle className="w-3 h-3" />
                        </Button>
                      </div>
                      <Link href={`/ws/${workspaceId}/t/${task.id}`}>
                      <div className="font-medium text-gray-200">{task.name}</div>
                      </Link>
                      <div className="flex items-center space-x-1 text-gray-400">
                        <Button variant="ghost" size="icon" className="w-6 h-6 text-gray-400 hover:bg-gray-700">
                          <Calendar className="w-3 h-3" />
                        </Button>
                        {task.dueDate && <span className="text-xs">{new Date(task.dueDate).toLocaleDateString()}</span>}
                      </div>
                      <div className="flex items-center space-x-1 text-gray-400">
                        <Button variant="ghost" size="icon" className="w-6 h-6 text-gray-400 hover:bg-gray-700">
                          <Flag className="w-3 h-3" />
                        </Button>
                        {task.priority !== undefined && <span className="text-xs">{task.priority}</span>}
                      </div>
                      <div>
                        <Badge
                          className={`${getStatusColor(task.status)} px-2 py-1 rounded-full text-xs font-semibold flex items-center gap-1`}
                        >
                          {getStatusIcon(task.status)}
                          {task.status}
                        </Badge>
                      </div>
                      <div className="flex items-center space-x-1 text-gray-400">
                        <Button variant="ghost" size="icon" className="w-6 h-6 text-gray-400 hover:bg-gray-700">
                          <MessageSquare className="w-3 h-3" />
                        </Button>
                      </div>
                    </div>
                  ))
                )}
              </div>
            )}
          </div>
        ))}
      </main>
      {/* Task Creation Form Dialog */}
      {isCreateFormOpen && (
        <CreateTaskForm
          isOpen={isCreateFormOpen}
          onClose={() => setIsCreateFormOpen(false)}
          initialStatus={initialStatusForNewTask}
          listId={listId || "default-list-id"} // Provide a default or handle undefined listId
        />
      )}
    </div>
  )
}
