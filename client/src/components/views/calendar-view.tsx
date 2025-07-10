import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { CalendarDays } from "lucide-react"
import type { Task } from "@/types/task"

interface CalendarViewProps {
  tasks: Task[]
}

export function CalendarView({ tasks }: CalendarViewProps) {
  return (
    <div className="h-full overflow-y-auto custom-scrollbar">
      <div className="p-6">
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-lg">
              <CalendarDays className="h-5 w-5" />
              Calendar View
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-7 gap-2 mb-4">
              {["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"].map((day) => (
                <div key={day} className="text-center text-sm font-medium p-2">
                  {day}
                </div>
              ))}
            </div>
            <div className="grid grid-cols-7 gap-2">
              {Array.from({ length: 35 }).map((_, i) => {
                const dayTasks = i === 10 ? [tasks[0]] : i === 15 ? [tasks[1]] : i === 20 ? [tasks[3]] : []
                return (
                  <div key={i} className="min-h-32 p-2 border rounded-lg hover:bg-muted/50">
                    <div className="text-sm font-medium mb-2">{i > 6 ? i - 6 : ""}</div>
                    <div className="space-y-1">
                      {dayTasks.map((task) => (
                        <div key={task.id} className="text-xs p-2 rounded bg-blue-100 dark:bg-blue-900">
                          <div className="font-medium truncate">{task.title}</div>
                          <div className="text-muted-foreground">{task.dueDate}</div>
                        </div>
                      ))}
                    </div>
                  </div>
                )
              })}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
