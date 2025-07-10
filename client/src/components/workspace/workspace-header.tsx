"use client"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Plus, Filter, Search, List, Kanban, CalendarDays, Table } from "lucide-react"

interface WorkspaceHeaderProps {
  selectedView: string
  onViewChange: (view: string) => void
}

export function WorkspaceHeader({ selectedView, onViewChange }: WorkspaceHeaderProps) {
  return (
    <div className="border-b bg-background p-4 flex-shrink-0">
      <div className="flex items-center justify-between mb-3">
        <h1 className="text-xl font-semibold">Tasks</h1>
        <div className="flex items-center gap-2">
          <div className="relative hidden md:block">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input placeholder="Search..." className="pl-9 w-64" />
          </div>
          <Button variant="outline" size="sm" className="hidden sm:flex bg-transparent">
            <Filter className="h-4 w-4 mr-2" />
            Filter
          </Button>
          <Dialog>
            <DialogTrigger asChild>
              <Button size="sm" className="gap-2">
                <Plus className="h-4 w-4" />
                <span className="hidden sm:inline">New Task</span>
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Create New Task</DialogTitle>
              </DialogHeader>
              <div className="space-y-4">
                <Input placeholder="Task name" />
                <Textarea placeholder="Description" rows={3} />
                <div className="grid grid-cols-2 gap-4">
                  <Input type="date" />
                  <select className="px-3 py-2 border rounded-md">
                    <option>High Priority</option>
                    <option>Medium Priority</option>
                    <option>Low Priority</option>
                  </select>
                </div>
                <Button className="w-full">Create Task</Button>
              </div>
            </DialogContent>
          </Dialog>
        </div>
      </div>

      <div className="flex items-center gap-1">
        <Button
          variant={selectedView === "list" ? "default" : "ghost"}
          size="sm"
          onClick={() => onViewChange("list")}
          className="gap-2"
        >
          <List className="h-4 w-4" />
          <span className="hidden sm:inline">List</span>
        </Button>
        <Button
          variant={selectedView === "board" ? "default" : "ghost"}
          size="sm"
          onClick={() => onViewChange("board")}
          className="gap-2"
        >
          <Kanban className="h-4 w-4" />
          <span className="hidden sm:inline">Board</span>
        </Button>
        <Button
          variant={selectedView === "table" ? "default" : "ghost"}
          size="sm"
          onClick={() => onViewChange("table")}
          className="gap-2"
        >
          <Table className="h-4 w-4" />
          <span className="hidden sm:inline">Table</span>
        </Button>
        <Button
          variant={selectedView === "calendar" ? "default" : "ghost"}
          size="sm"
          onClick={() => onViewChange("calendar")}
          className="gap-2"
        >
          <CalendarDays className="h-4 w-4" />
          <span className="hidden sm:inline">Calendar</span>
        </Button>
      </div>
    </div>
  )
}
