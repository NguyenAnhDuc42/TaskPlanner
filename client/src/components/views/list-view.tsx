import { Badge } from "@/components/ui/badge"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Flag } from "lucide-react"
import type { Task } from "@/types/task"
import { getStatusColor, getPriorityColor } from "@/utils/task-helpers"

interface ListViewProps {
  tasks: Task[]
}

export function ListView({ tasks }: ListViewProps) {
  return (
    <div className="h-full overflow-y-auto custom-scrollbar">
      <div className="p-6">
        <div className="space-y-2">
          {tasks.map((task) => (
            <div
              key={task.id}
              className="flex items-center gap-4 p-4 hover:bg-muted/50 rounded-lg border border-transparent hover:border-border"
            >
              <div className={`w-3 h-3 rounded-full ${getStatusColor(task.status)}`} />
              <div className="flex-1 min-w-0">
                <h4 className="font-medium text-sm mb-1">{task.title}</h4>
                <p className="text-xs text-muted-foreground line-clamp-2">{task.description}</p>
              </div>
              <div className="flex items-center gap-2">
                <Badge variant="outline" className="text-xs hidden md:inline-flex">
                  {task.space}
                </Badge>
                <Flag className={`h-3 w-3 ${getPriorityColor(task.priority)}`} />
                <div className="text-xs text-muted-foreground w-16 text-right">{task.dueDate}</div>
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
                <div className="text-xs text-muted-foreground w-12 text-right">{task.progress}%</div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
