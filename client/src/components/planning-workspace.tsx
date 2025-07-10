"use client"
import { useState } from "react"
import type { Task } from "@/types/task"
import { initialTasks } from "@/data/tasks"
import { WorkspaceHeader } from "@/components/workspace/workspace-header"
import { ListView } from "@/components/views/list-view"
import { BoardView } from "@/components/views/board-view"

export function PlanningWorkspace() {
  const [selectedView, setSelectedView] = useState("board")
  const [tasks, setTasks] = useState<Task[]>(initialTasks)

  const handleTaskUpdate = (taskId: number, updates: Partial<Task>) => {
    setTasks((prevTasks) => prevTasks.map((task) => (task.id === taskId ? { ...task, ...updates } : task)))
  }

  return (
    <div className="h-full w-full flex flex-col overflow-hidden">
      {/* Custom scrollbar styles */}
      <style jsx global>{`
        .custom-scrollbar {
          scrollbar-width: thin;
          scrollbar-color: hsl(var(--border)) transparent;
        }
        
        .custom-scrollbar::-webkit-scrollbar {
          width: 6px;
          height: 6px;
        }
        
        .custom-scrollbar::-webkit-scrollbar-track {
          background: transparent;
        }
        
        .custom-scrollbar::-webkit-scrollbar-thumb {
          background-color: hsl(var(--border));
          border-radius: 3px;
          border: none;
        }
        
        .custom-scrollbar::-webkit-scrollbar-thumb:hover {
          background-color: hsl(var(--muted-foreground));
        }

        .drag-over {
          background-color: hsl(var(--primary) / 0.1);
          border-color: hsl(var(--primary));
        }
      `}</style>

      {/* Top Bar */}
      <WorkspaceHeader selectedView={selectedView} onViewChange={setSelectedView} />

      {/* Content Area */}
      <div className="flex-1 overflow-hidden">
        {/* LIST VIEW */}
        {selectedView === "list" && <ListView tasks={tasks} />}

        {/* BOARD VIEW */}
        {selectedView === "board" && <BoardView tasks={tasks} onTaskUpdate={handleTaskUpdate} />}

        {/* TABLE VIEW */}
        {selectedView === "table" && (
          <div className="h-full flex items-center justify-center text-muted-foreground">Table view coming soon...</div>
        )}

        {/* CALENDAR VIEW */}
        {selectedView === "calendar" && (
          <div className="h-full flex items-center justify-center text-muted-foreground">
            Calendar view coming soon...
          </div>
        )}
      </div>
    </div>
  )
}
