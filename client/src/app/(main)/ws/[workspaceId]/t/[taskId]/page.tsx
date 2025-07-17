"use client"

import { useState, useEffect, useCallback, type ReactNode, use } from "react"
import type { PlanTaskStatus } from "@/types/task"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Badge } from "@/components/ui/badge"
import { Calendar, Clock, Flag, Archive, Eye, Trash2, Edit2, CircleDot } from "lucide-react"
import { useGetTask, useUpdateTask, useDeleteTask } from "@/features/task/task-hooks"
import { UpdateTaskBodyRequest } from "@/features/task/task-type"

export default function TaskPage({ params }: { params: Promise<{ taskId: string }> }) {
  const resolvedParams = use(params)
  const taskId = resolvedParams.taskId

  const { data: task, isLoading, error } = useGetTask(taskId)
  const updateTask = useUpdateTask()
  const deleteTask = useDeleteTask()

  const [isEditMode, setIsEditMode] = useState(false)
  const [editableTaskData, setEditableTaskData] = useState<UpdateTaskBodyRequest | null>(null)

  // Initialize editableTaskData when task loads or changes
  useEffect(() => {
    if (task) {
      console.log("[TaskPage] Task data received from API:", task);
      // Only include fields that are part of UpdateTaskBodyRequest
      const initialEditableData = {
        name: task.name,
        description: task.description,
        priority: task.priority,
        status: task.status,
        startDate: task.startDate,
        dueDate: task.dueDate,
        timeEstimate: task.timeEstimate,
        timeSpent: task.timeSpent,
        orderIndex: task.orderIndex,
        isArchived: task.isArchived,
        isPrivate: task.isPrivate,
        listId: task.listId, // Assuming listId can be part of update
      };
      console.log("[TaskPage] Setting initial editable data state:", initialEditableData);
      setEditableTaskData(initialEditableData)
    }
  }, [task])

  const handleFormChange = useCallback(
    (field: keyof UpdateTaskBodyRequest, value: string | number | boolean | null | undefined) => {
      console.log(`[TaskPage] Form field changed:`, { field, value });
      setEditableTaskData((prev) => {
        if (!prev) return null
        const newState = {
          ...prev,
          [field]: value,
        };
        console.log("[TaskPage] New editableTaskData state:", newState);
        return newState;
      })
    },
    [],
  )

 const handleSaveAllChanges = async () => {
    if (!editableTaskData || !task) return
    
    console.log("[TaskPage] State before creating payload:", editableTaskData);
    const payload: UpdateTaskBodyRequest = {
      ...Object.fromEntries(
        Object.entries(editableTaskData).filter(([, value]) => value !== null),
      ),
      name: editableTaskData.name ?? "Untitled Task", // Provide a default if name is null
    };

    console.log("[TaskPage] Final payload being sent to API:", payload);

    await updateTask.mutateAsync({id: taskId,data: payload,})
    setIsEditMode(false) // Exit edit mode after saving
  }

  const handleCancelEdit = () => {
    if (task) {
      // Reset to original task data
      setEditableTaskData({
        name: task.name,
        description: task.description,
        priority: task.priority,
        status: task.status,
        startDate: task.startDate,
        dueDate: task.dueDate,
        timeEstimate: task.timeEstimate,
        timeSpent: task.timeSpent,
        orderIndex: task.orderIndex,
        isArchived: task.isArchived,
        isPrivate: task.isPrivate,
        listId: task.listId,
      })
    }
    setIsEditMode(false) // Exit edit mode
  }

  const handleDelete = async () => {
    if (window.confirm("Are you sure you want to delete this task?")) {
      await deleteTask.mutateAsync(taskId)
    }
  }

  const getStatusColor = (status: PlanTaskStatus) => {
    switch (status) {
      case "ToDo":
        return "bg-neutral-700 text-neutral-100"
      case "InProgress":
        return "bg-blue-700 text-blue-100"
      case "InReview":
        return "bg-yellow-700 text-yellow-100"
      case "Done":
        return "bg-green-700 text-green-100"
      default:
        return "bg-neutral-700 text-neutral-100"
    }
  }

  const getPriorityColor = (priority: number) => {
    if (priority >= 4) return "text-red-400"
    if (priority >= 3) return "text-orange-400"
    if (priority >= 2) return "text-yellow-400"
    return "text-green-400"
  }

  const formatDate = (dateString: string | null | undefined): ReactNode => {
    if (typeof dateString !== "string" || !dateString) return "Empty"
    try {
      const date = new Date(dateString)
      return (
        <>
          <Calendar className="inline-block h-4 w-4 mr-1 text-neutral-400" />
          {date.toLocaleDateString("en-US", { month: "short", day: "numeric" })}
        </>
      )
    } catch {
      return "Invalid Date"
    }
  }

  const formatTime = (minutes: number | null | undefined): ReactNode => {
    if (minutes === null || minutes === undefined) return "Empty"
    const hours = Math.floor(minutes / 60)
    const mins = minutes % 60
    return `${hours}h ${mins}m`
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-neutral-950 text-neutral-100">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-purple-500"></div>
      </div>
    )
  }

  if (error || !task || !editableTaskData) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-neutral-950 text-neutral-100">
        <div className="text-center">
          <h2 className="text-2xl font-bold text-neutral-100 mb-2">Task not found</h2>
          <p className="text-neutral-400">The task youre looking for doesnt exist or has been deleted.</p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-neutral-950 text-neutral-100 p-6">
      <div className="max-w-5xl mx-auto space-y-8">
        {/* Header and Title */}
        <div className="flex items-center justify-between">
          <h1 className="text-4xl font-bold text-neutral-100">
            {isEditMode ? (
              <Input
                value={editableTaskData.name || ""}
                onChange={(e) => handleFormChange("name", e.target.value)}
                className="bg-neutral-950 border-neutral-800 text-neutral-100 text-4xl font-bold focus:ring-1 focus:ring-purple-500 focus:border-purple-500"
              />
            ) : (
              task.name || "Untitled Task"
            )}
          </h1>
          <div className="flex items-center gap-2">
            {isEditMode ? (
              <>
                <Button
                  onClick={handleSaveAllChanges}
                  disabled={updateTask.isPending}
                  className="bg-purple-600 hover:bg-purple-700 text-white"
                >
                  {updateTask.isPending ? "Saving..." : "Save Changes"}
                </Button>
                <Button
                  variant="outline"
                  onClick={handleCancelEdit}
                  className="border-neutral-700 text-neutral-100 hover:bg-neutral-800 bg-transparent"
                >
                  Cancel
                </Button>
              </>
            ) : (
              <Button
                onClick={() => setIsEditMode(true)}
                variant="outline"
                className="border-neutral-700 text-neutral-100 hover:bg-neutral-800"
              >
                <Edit2 className="h-4 w-4 mr-2" />
                Edit Mode
              </Button>
            )}
            <Button
              variant="destructive"
              size="sm"
              onClick={handleDelete}
              className="bg-red-600 hover:bg-red-700 text-white"
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        </div>

        {/* Properties Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-x-8 gap-y-4">
          {/* Left Column */}
          <div className="space-y-2">
            <div className="flex items-center gap-2 py-2">
              <CircleDot className="h-4 w-4 text-neutral-400 shrink-0" />
              <span className="text-neutral-400 text-sm w-24 shrink-0">Status:</span>
              <div className="flex-1">
                {isEditMode ? (
                  <Select
                    value={editableTaskData.status}
                    onValueChange={(val) => handleFormChange("status", val as PlanTaskStatus)}
                  >
                    <SelectTrigger className="w-full bg-neutral-800 border-neutral-700 text-neutral-100 focus:ring-1 focus:ring-purple-500 focus:border-purple-500">
                      <SelectValue placeholder="Select status" />
                    </SelectTrigger>
                    <SelectContent className="bg-neutral-800 text-neutral-100 border-neutral-700">
                      <SelectItem value="ToDo">To Do</SelectItem>
                      <SelectItem value="InProgress">In Progress</SelectItem>
                      <SelectItem value="InReview">In Review</SelectItem>
                      <SelectItem value="Done">Done</SelectItem>
                    </SelectContent>
                  </Select>
                ) : (
                  <Badge className={getStatusColor(task.status)}>
                    {task.status === "ToDo"
                      ? "To Do"
                      : task.status === "InProgress"
                        ? "In Progress"
                        : task.status === "InReview"
                          ? "In Review"
                          : task.status}
                  </Badge>
                )}
              </div>
            </div>

            <div className="flex items-center gap-2 py-2">
              <Calendar className="h-4 w-4 text-neutral-400 shrink-0" />
              <span className="text-neutral-400 text-sm w-24 shrink-0">Dates:</span>
              <div className="flex-1">
                {isEditMode ? (
                  <Input
                    type="date"
                    value={editableTaskData.startDate || ""}
                    onChange={(e) => handleFormChange("startDate", e.target.value)}
                    className="w-full bg-neutral-800 border-neutral-700 text-neutral-100 focus:ring-1 focus:ring-purple-500 focus:border-purple-500"
                  />
                ) : (
                  <span className="text-neutral-100">{formatDate(task.startDate)}</span>
                )}
              </div>
            </div>

            <div className="flex items-center gap-2 py-2">
              <Clock className="h-4 w-4 text-neutral-400 shrink-0" />
              <span className="text-neutral-400 text-sm w-24 shrink-0">Time Estimate:</span>
              <div className="flex-1">
                {isEditMode ? (
                  <Input
                    type="number"
                    value={editableTaskData.timeEstimate || ""}
                    onChange={(e) =>
                      handleFormChange("timeEstimate", e.target.value === "" ? null : Number(e.target.value))
                    }
                    className="w-full bg-neutral-800 border-neutral-700 text-neutral-100 focus:ring-1 focus:ring-purple-500 focus:border-purple-500"
                  />
                ) : (
                  <span className="text-neutral-100">{formatTime(task.timeEstimate)}</span>
                )}
              </div>
            </div>

            <div className="flex items-center gap-2 py-2">
              <Eye className="h-4 w-4 text-neutral-400 shrink-0" />
              <span className="text-neutral-400 text-sm w-24 shrink-0">Private:</span>
              <div className="flex-1">
                {isEditMode ? (
                  <input
                    type="checkbox"
                    checked={editableTaskData.isPrivate}
                    onChange={(e) => handleFormChange("isPrivate", e.target.checked)}
                    className="h-4 w-4 accent-purple-500"
                  />
                ) : (
                  <span className="text-neutral-100">{task.isPrivate ? "Yes" : "No"}</span>
                )}
              </div>
            </div>
          </div>

          {/* Right Column */}
          <div className="space-y-2">
            <div className="flex items-center gap-2 py-2">
              <Flag className="h-4 w-4 text-neutral-400 shrink-0" />
              <span className="text-neutral-400 text-sm w-24 shrink-0">Priority:</span>
              <div className="flex-1">
                {isEditMode ? (
                  <Select
                    value={String(editableTaskData.priority)}
                    onValueChange={(val) => handleFormChange("priority", Number(val))}
                  >
                    <SelectTrigger className="w-full bg-neutral-800 border-neutral-700 text-neutral-100 focus:ring-1 focus:ring-purple-500 focus:border-purple-500">
                      <SelectValue placeholder="Select priority" />
                    </SelectTrigger>
                    <SelectContent className="bg-neutral-800 text-neutral-100 border-neutral-700">
                      <SelectItem value="1">Low (1)</SelectItem>
                      <SelectItem value="2">Normal (2)</SelectItem>
                      <SelectItem value="3">High (3)</SelectItem>
                      <SelectItem value="4">Urgent (4)</SelectItem>
                      <SelectItem value="5">Critical (5)</SelectItem>
                    </SelectContent>
                  </Select>
                ) : (
                  <span className={`text-neutral-100 ${getPriorityColor(task.priority)}`}>
                    Priority {task.priority}
                  </span>
                )}
              </div>
            </div>

            <div className="flex items-center gap-2 py-2">
              <Calendar className="h-4 w-4 text-neutral-400 shrink-0" />
              <span className="text-neutral-400 text-sm w-24 shrink-0">Due Date:</span>
              <div className="flex-1">
                {isEditMode ? (
                  <Input
                    type="date"
                    value={editableTaskData.dueDate || ""}
                    onChange={(e) => handleFormChange("dueDate", e.target.value)}
                    className="w-full bg-neutral-800 border-neutral-700 text-neutral-100 focus:ring-1 focus:ring-purple-500 focus:border-purple-500"
                  />
                ) : (
                  <span className="text-neutral-100">{formatDate(task.dueDate)}</span>
                )}
              </div>
            </div>

            <div className="flex items-center gap-2 py-2">
              <Clock className="h-4 w-4 text-neutral-400 shrink-0" />
              <span className="text-neutral-400 text-sm w-24 shrink-0">Time Spent:</span>
              <div className="flex-1">
                {isEditMode ? (
                  <Input
                    type="number"
                    value={editableTaskData.timeSpent || ""}
                    onChange={(e) =>
                      handleFormChange("timeSpent", e.target.value === "" ? null : Number(e.target.value))
                    }
                    className="w-full bg-neutral-800 border-neutral-700 text-neutral-100 focus:ring-1 focus:ring-purple-500 focus:border-purple-500"
                  />
                ) : (
                  <span className="text-neutral-100">{formatTime(task.timeSpent)}</span>
                )}
              </div>
            </div>

            <div className="flex items-center gap-2 py-2">
              <Archive className="h-4 w-4 text-neutral-400 shrink-0" />
              <span className="text-neutral-400 text-sm w-24 shrink-0">Archived:</span>
              <div className="flex-1">
                {isEditMode ? (
                  <input
                    type="checkbox"
                    checked={editableTaskData.isArchived}
                    onChange={(e) => handleFormChange("isArchived", e.target.checked)}
                    className="h-4 w-4 accent-purple-500"
                  />
                ) : (
                  <span className="text-neutral-100">{task.isArchived ? "Yes" : "No"}</span>
                )}
              </div>
            </div>
          </div>
        </div>

        {/* Description */}
        <div className="space-y-2">
          <h3 className="text-lg font-semibold text-neutral-100">Description</h3>
          {isEditMode ? (
            <Textarea
              value={editableTaskData.description || ""}
              onChange={(e) => handleFormChange("description", e.target.value)}
              className="w-full bg-neutral-800 border-neutral-700 text-neutral-100 focus:ring-1 focus:ring-purple-500 focus:border-purple-500"
              rows={5}
              placeholder="Add description"
            />
          ) : (
            <p className="text-neutral-300">{task.description || "Add description"}</p>
          )}
        </div>
      </div>
    </div>
  )
}
