"use client"

import { useState, useEffect, useCallback, ReactNode, use } from "react"

import type { PlanTaskStatus } from "@/types/task"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader } from "@/components/ui/card"
import { Calendar, Clock, Flag, User, Archive, Eye, EyeOff, Trash2, Edit2, Check, X } from "lucide-react"
import { UpdateTaskBodyRequest } from "@/features/task/task-type"
import { useDeleteTask, useGetTask, useUpdateTask } from "@/features/task/task-hooks"


// Define a more specific type for the value prop in EditableField
type EditableFieldValue = string | number | boolean | null | undefined

interface EditableFieldProps {
  field: keyof UpdateTaskBodyRequest // Use keyof UpdateTaskBodyRequest for field names
  value: EditableFieldValue
  type?: "text" | "select" | "date" | "number" | "boolean"
  options?: { value: EditableFieldValue; label: string }[]
  multiline?: boolean
  formatter?: (value: EditableFieldValue) => ReactNode
  onUpdate: (field: keyof UpdateTaskBodyRequest, newValue: EditableFieldValue) => Promise<void>
  isUpdating: boolean // Prop to indicate if the parent mutation is pending
}

// Refactored EditableField to manage its own internal state and editing logic
const EditableField = ({
  field,
  value,
  type = "text",
  options = [],
  multiline = false,
  formatter = (v) => (v !== null && v !== undefined && v !== "" ? String(v) : "Not set"), // Improved default formatter
  onUpdate,
  isUpdating,
}: EditableFieldProps) => {
  const [isEditing, setIsEditing] = useState(false)
  const [internalValue, setInternalValue] = useState<EditableFieldValue>(value)

  // Sync internalValue with external value when not editing (e.g., after successful save or initial load)
  useEffect(() => {
    if (!isEditing) {
      setInternalValue(value)
    }
  }, [value, isEditing])

  const handleSave = useCallback(async () => {
    await onUpdate(field, internalValue)
    setIsEditing(false) // Exit edit mode after save
  }, [field, internalValue, onUpdate])

  const handleCancel = useCallback(() => {
    setInternalValue(value) // Reset to original value
    setIsEditing(false) // Exit edit mode
  }, [value])

  if (isEditing) {
    return (
      <div className="flex items-center gap-2 w-full">
        {type === "select" ? (
          <Select
            value={String(internalValue)} // Select expects string value
            onValueChange={(val) => {
              // Convert back to original type if necessary, e.g., number for priority
              const typedVal =
                field === "priority" || field === "timeEstimate" || field === "timeSpent" || field === "orderIndex"
                  ? Number(val)
                  : val
              setInternalValue(typedVal)
            }}
          >
            <SelectTrigger className="w-full">
              <SelectValue placeholder="Select..." />
            </SelectTrigger>
            <SelectContent>
              {options.map((option) => (
                <SelectItem key={String(option.value)} value={String(option.value)}>
                  {option.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        ) : multiline ? (
          <Textarea
            value={String(internalValue || "")}
            onChange={(e) => setInternalValue(e.target.value)}
            className="flex-1"
            rows={3}
          />
        ) : (
          <Input
            type={type === "number" ? "number" : type === "date" ? "date" : "text"}
            value={String(internalValue || "")}
            onChange={(e) => {
              const val = e.target.value
              const typedVal = type === "number" ? (val === "" ? null : Number(val)) : val
              setInternalValue(typedVal)
            }}
            className="flex-1"
          />
        )}
        <Button size="sm" onClick={handleSave} disabled={isUpdating}>
          <Check className="h-4 w-4" />
        </Button>
        <Button size="sm" variant="outline" onClick={handleCancel}>
          <X className="h-4 w-4" />
        </Button>
      </div>
    )
  }

  return (
    <div
      className="group flex items-center gap-2 cursor-pointer hover:bg-gray-100 p-2 rounded w-full" // Changed hover color for better contrast
      onClick={() => setIsEditing(true)} // Enter edit mode on click
    >
      <span className="flex-1 text-gray-900">{formatter(value)}</span> {/* Ensure text color is readable */}
      <Edit2 className="h-4 w-4 opacity-0 group-hover:opacity-100 transition-opacity" />
    </div>
  )
}

export default function TaskPage({ params }: { params: Promise<{ taskId: string }> }) {
  const resolvedParams = use(params); 
  const taskId = resolvedParams.taskId;
  const { data: task, isLoading, error } = useGetTask(taskId)
  const updateTask = useUpdateTask()
  const deleteTask = useDeleteTask()

  const handleUpdateField = useCallback(
    async (field: keyof UpdateTaskBodyRequest, newValue: EditableFieldValue) => {
      const updatePayload: { id: string; data: UpdateTaskBodyRequest } = {
        id: taskId,
        data: { [field]: newValue } as UpdateTaskBodyRequest, // Cast to ensure type compatibility
      }
      await updateTask.mutateAsync(updatePayload)
    },
    [taskId, updateTask],
  )

  const handleDelete = async () => {
    if (window.confirm("Are you sure you want to delete this task?")) {
      await deleteTask.mutateAsync(taskId)
    }
  }

  const getStatusColor = (status: PlanTaskStatus) => {
    switch (status) {
      case "ToDo":
        return "bg-gray-100 text-gray-800"
      case "InProgress":
        return "bg-blue-100 text-blue-800"
      case "InReview":
        return "bg-yellow-100 text-yellow-800"
      case "Done":
        return "bg-green-100 text-green-800"
      default:
        return "bg-gray-100 text-gray-800"
    }
  }

  const getPriorityColor = (priority: number) => {
    if (priority >= 4) return "text-red-500"
    if (priority >= 3) return "text-orange-500"
    if (priority >= 2) return "text-yellow-500"
    return "text-green-500"
  }

  const formatDate = (dateString: string | null | undefined) => {
    if (!dateString) return "Not set"
    try {
      return new Date(dateString).toLocaleDateString()
    } catch {
      return "Invalid Date"
    }
  }

  const formatTime = (minutes: number | null | undefined) => {
    if (minutes === null || minutes === undefined) return "Not set"
    const hours = Math.floor(minutes / 60)
    const mins = minutes % 60
    return `${hours}h ${mins}m`
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    )
  }

  if (error || !task) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <h2 className="text-2xl font-bold text-gray-900 mb-2">Task not found</h2>
          <p className="text-gray-600">The task youre looking for doesnt exist or has been deleted.</p>
        </div>
      </div>
    )
  }

  return (
    <div className="max-w-4xl mx-auto p-6 space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <EditableField
            field="name"
            value={task.name}
            formatter={(v) => (v ? String(v) : "Untitled Task")}
            onUpdate={handleUpdateField}
            isUpdating={updateTask.isPending}
          />
        </div>
        <div className="flex items-center gap-2 ml-4">
          {/* For boolean toggles, we can directly call handleUpdateField */}
          <Button variant="outline" size="sm" onClick={() => handleUpdateField("isPrivate", !task.isPrivate)}>
            {task.isPrivate ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            {task.isPrivate ? "Private" : "Public"}
          </Button>
          <Button variant="outline" size="sm" onClick={() => handleUpdateField("isArchived", !task.isArchived)}>
            <Archive className="h-4 w-4" />
            {task.isArchived ? "Unarchive" : "Archive"}
          </Button>
          <Button variant="destructive" size="sm" onClick={handleDelete}>
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {/* Status and Priority */}
      <div className="flex items-center gap-4">
        <div className="flex items-center gap-2">
          <span className="text-sm font-medium">Status:</span>
          <EditableField
            field="status"
            value={task.status}
            type="select"
            options={[
              { value: "ToDo", label: "To Do" },
              { value: "InProgress", label: "In Progress" },
              { value: "InReview", label: "In Review" },
              { value: "Done", label: "Done" },
            ]}
            formatter={(v) => (
              <Badge className={getStatusColor(v as PlanTaskStatus)}>
                {v === "ToDo"
                  ? "To Do"
                  : v === "InProgress"
                    ? "In Progress"
                    : v === "InReview"
                      ? "In Review"
                      : String(v)}
              </Badge>
            )}
            onUpdate={handleUpdateField}
            isUpdating={updateTask.isPending}
          />
        </div>
        <div className="flex items-center gap-2">
          <Flag className={`h-4 w-4 ${getPriorityColor(task.priority)}`} />
          <span className="text-sm font-medium">Priority:</span>
          <EditableField
            field="priority"
            value={task.priority}
            type="select"
            options={[
              { value: 1, label: "Low (1)" },
              { value: 2, label: "Normal (2)" },
              { value: 3, label: "High (3)" },
              { value: 4, label: "Urgent (4)" },
              { value: 5, label: "Critical (5)" },
            ]}
            formatter={(v) => `Priority ${v}`}
            onUpdate={handleUpdateField}
            isUpdating={updateTask.isPending}
          />
        </div>
      </div>

      {/* Description */}
      <Card>
        <CardHeader>
          <h3 className="text-lg font-semibold">Description</h3>
        </CardHeader>
        <CardContent>
          <EditableField
            field="description"
            value={task.description}
            multiline
            formatter={(v) => (v ? String(v) : "No description provided")}
            onUpdate={handleUpdateField}
            isUpdating={updateTask.isPending}
          />
        </CardContent>
      </Card>

      {/* Dates and Time */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <h3 className="text-lg font-semibold flex items-center gap-2">
              <Calendar className="h-5 w-5" />
              Dates
            </h3>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="text-sm font-medium text-gray-600">Start Date</label>
              <EditableField
                field="startDate"
                value={task.startDate}
                type="date"
                formatter={formatDate}
                onUpdate={handleUpdateField}
                isUpdating={updateTask.isPending}
              />
            </div>
            <div>
              <label className="text-sm font-medium text-gray-600">Due Date</label>
              <EditableField
                field="dueDate"
                value={task.dueDate}
                type="date"
                formatter={formatDate}
                onUpdate={handleUpdateField}
                isUpdating={updateTask.isPending}
              />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <h3 className="text-lg font-semibold flex items-center gap-2">
              <Clock className="h-5 w-5" />
              Time Tracking
            </h3>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="text-sm font-medium text-gray-600">Time Estimate (minutes)</label>
              <EditableField
                field="timeEstimate"
                value={task.timeEstimate}
                type="number"
                formatter={formatTime}
                onUpdate={handleUpdateField}
                isUpdating={updateTask.isPending}
              />
            </div>
            <div>
              <label className="text-sm font-medium text-gray-600">Time Spent (minutes)</label>
              <EditableField
                field="timeSpent"
                value={task.timeSpent}
                type="number"
                formatter={formatTime}
                onUpdate={handleUpdateField}
                isUpdating={updateTask.isPending}
              />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Additional Info */}
      <Card>
        <CardHeader>
          <h3 className="text-lg font-semibold flex items-center gap-2">
            <User className="h-5 w-5" />
            Additional Information
          </h3>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="text-sm font-medium text-gray-600">Order Index</label>
              <EditableField
                field="orderIndex"
                value={task.orderIndex}
                type="number"
                formatter={(v) => `Position ${v}`}
                onUpdate={handleUpdateField}
                isUpdating={updateTask.isPending}
              />
            </div>
            <div>
              <label className="text-sm font-medium text-gray-600">Creator ID</label>
              <div className="p-2 bg-gray-50 rounded text-sm text-gray-600">{task.creatorId}</div>
            </div>
          </div>
          <div>
            <label className="text-sm font-medium text-gray-600">List ID</label>
            <div className="p-2 bg-gray-50 rounded text-sm text-gray-600">{task.listId}</div>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
