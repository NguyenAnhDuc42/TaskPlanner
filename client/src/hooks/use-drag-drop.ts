"use client"

import { useState, useCallback } from "react"
import type { Task } from "@/types/task"

export interface DragDropHandlers {
  draggedTask: Task | null
  dragOverColumn: string | null
  handleDragStart: (task: Task) => void
  handleDragEnd: () => void
  handleDragEnter: (status: string) => void
  handleDragLeave: () => void
  handleDrop: (newStatus: string) => void
}

export function useDragDrop(onTaskUpdate: (taskId: number, updates: Partial<Task>) => void): DragDropHandlers {
  const [draggedTask, setDraggedTask] = useState<Task | null>(null)
  const [dragOverColumn, setDragOverColumn] = useState<string | null>(null)

  const handleDragStart = useCallback((task: Task) => {
    setDraggedTask(task)
  }, [])

  const handleDragEnd = useCallback(() => {
    setDraggedTask(null)
    setDragOverColumn(null)
  }, [])

  const handleDragEnter = useCallback((status: string) => {
    setDragOverColumn(status)
  }, [])

  const handleDragLeave = useCallback(() => {
    setDragOverColumn(null)
  }, [])

  const handleDrop = useCallback(
    (newStatus: string) => {
      if (draggedTask && draggedTask.status !== newStatus) {
        onTaskUpdate(draggedTask.id, { status: newStatus as Task["status"] })
      }
      handleDragEnd()
    },
    [draggedTask, onTaskUpdate, handleDragEnd],
  )

  return {
    draggedTask,
    dragOverColumn,
    handleDragStart,
    handleDragEnd,
    handleDragEnter,
    handleDragLeave,
    handleDrop,
  }
}
