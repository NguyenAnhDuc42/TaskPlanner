"use client"

import type React from "react"
import { Card, CardContent } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Plus, MoreHorizontal, Flag, MessageSquare } from "lucide-react"
import type { Task } from "@/types/task"
import { getStatusColor, getPriorityColor } from "@/utils/task-helpers"
import { useDragDrop } from "@/hooks/use-drag-drop"

interface BoardViewProps {
  tasks: Task[]
  onTaskUpdate: (taskId: number, updates: Partial<Task>) => void
}

export function BoardView({ tasks, onTaskUpdate }: BoardViewProps) {
  const { draggedTask, dragOverColumn, handleDragStart, handleDragEnd, handleDragEnter, handleDragLeave, handleDrop } =
    useDragDrop(onTaskUpdate)

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault()
    e.dataTransfer.dropEffect = "move"
  }

  const handleColumnDragEnter = (e: React.DragEvent, status: string) => {
    e.preventDefault()
    handleDragEnter(status)
  }

  const handleColumnDragLeave = (e: React.DragEvent) => {
    const rect = (e.currentTarget as HTMLElement).getBoundingClientRect()
    const x = e.clientX
    const y = e.clientY

    if (x < rect.left || x > rect.right || y < rect.top || y > rect.bottom) {
      handleDragLeave()
    }
  }

  const handleColumnDrop = (e: React.DragEvent, newStatus: string) => {
    e.preventDefault()
    handleDrop(newStatus)
  }

  const TrelloCard = ({ task }: { task: Task }) => (
    <Card
      draggable
      onDragStart={() => handleDragStart(task)}
      onDragEnd={handleDragEnd}
      className={`mb-2 hover:shadow-md transition-all cursor-grab active:cursor-grabbing group bg-card border border-border select-none ${
        draggedTask?.id === task.id ? "shadow-lg scale-105" : ""
      }`}
    >
      <CardContent className="p-3">
        <div className="flex items-start justify-between mb-2">
          <h4 className="font-medium text-sm flex-1 leading-tight pr-2">{task.title}</h4>
          <Button variant="ghost" size="sm" className="h-5 w-5 p-0 opacity-0 group-hover:opacity-100">
            <MoreHorizontal className="h-3 w-3" />
          </Button>
        </div>

        {task.tags.length > 0 && (
          <div className="flex items-center gap-1 mb-2 flex-wrap">
            {task.tags.slice(0, 2).map((tag: string, i: number) => (
              <Badge key={i} variant="secondary" className="text-xs px-1.5 py-0.5 h-5">
                {tag}
              </Badge>
            ))}
          </div>
        )}

        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Flag className={`h-3 w-3 ${getPriorityColor(task.priority)}`} />
            <span className="text-xs text-muted-foreground">{task.dueDate}</span>
          </div>

          <div className="flex items-center gap-2">
            {task.comments > 0 && (
              <div className="flex items-center gap-1 text-xs text-muted-foreground">
                <MessageSquare className="h-3 w-3" />
                <span>{task.comments}</span>
              </div>
            )}
            <div className="flex -space-x-1">
              {task.assignees.slice(0, 2).map((assignee: string, i: number) => (
                <Avatar key={i} className="h-6 w-6 border-2 border-background">
                  <AvatarImage src="/placeholder.svg" />
                  <AvatarFallback className="text-xs">
                    {assignee
                      .split(" ")
                      .map((n: string) => n[0])
                      .join("")}
                  </AvatarFallback>
                </Avatar>
              ))}
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  )

  return (
    <div className="h-full overflow-x-auto overflow-y-hidden custom-scrollbar">
      <div className="h-full p-6">
        <div className="flex gap-4 h-full min-w-max">
          {["todo", "in-progress", "in-review", "completed"].map((status) => (
            <div
              key={status}
              className={`flex flex-col w-72 bg-muted/30 rounded-lg p-3 h-full transition-all duration-200 ${
                dragOverColumn === status ? "bg-primary/10 border-2 border-primary border-dashed" : ""
              }`}
              onDragOver={handleDragOver}
              onDragEnter={(e) => handleColumnDragEnter(e, status)}
              onDragLeave={handleColumnDragLeave}
              onDrop={(e) => handleColumnDrop(e, status)}
            >
              <div className="flex items-center justify-between mb-3 flex-shrink-0">
                <div className="flex items-center gap-2">
                  <div className={`w-3 h-3 rounded-full ${getStatusColor(status)}`} />
                  <h3 className="font-medium text-sm capitalize">{status.replace("-", " ")}</h3>
                </div>
                <div className="flex items-center gap-1">
                  <Badge variant="secondary" className="text-xs">
                    {tasks.filter((t) => t.status === status).length}
                  </Badge>
                  <Button variant="ghost" size="sm" className="h-6 w-6 p-0">
                    <Plus className="h-3 w-3" />
                  </Button>
                </div>
              </div>

              <div className="flex-1 overflow-y-auto custom-scrollbar pr-1">
                <div className="space-y-2">
                  {tasks
                    .filter((task) => task.status === status)
                    .map((task) => (
                      <TrelloCard key={task.id} task={task} />
                    ))}
                </div>
              </div>

              <Button
                variant="ghost"
                size="sm"
                className="mt-2 justify-start text-muted-foreground hover:text-foreground flex-shrink-0"
              >
                <Plus className="h-3 w-3 mr-2" />
                Add a card
              </Button>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
