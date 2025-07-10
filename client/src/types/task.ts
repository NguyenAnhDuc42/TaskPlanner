import type React from "react"
export interface Task {
  id: number
  title: string
  description: string
  status: "todo" | "in-progress" | "in-review" | "completed"
  priority: "low" | "medium" | "high" | "urgent"
  assignees: string[]
  dueDate: string
  progress: number
  space: string
  tags: string[]
  timeTracked: string
  comments: number
  attachments: number
}

export interface TaskCardProps {
  task: Task
  onDragStart?: (e: React.DragEvent, task: Task) => void
  onDragEnd?: () => void
}
