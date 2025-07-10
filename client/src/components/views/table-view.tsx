import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Progress } from "@/components/ui/progress"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Flag, ArrowUpDown } from "lucide-react"
import type { Task } from "@/types/task"
import { getStatusColor, getPriorityColor } from "@/utils/task-helpers"

interface TableViewProps {
  tasks: Task[]
}

export function TableView({ tasks }: TableViewProps) {
  return (
    <div className="h-full overflow-auto custom-scrollbar">
      <div className="p-6">
        <div className="border rounded-lg overflow-auto custom-scrollbar">
          <table className="w-full min-w-[800px]">
            <thead className="bg-muted/50 sticky top-0">
              <tr className="border-b">
                <th className="text-left p-3 font-medium text-sm">
                  <Button variant="ghost" size="sm" className="h-auto p-0 font-medium">
                    Task <ArrowUpDown className="ml-1 h-3 w-3" />
                  </Button>
                </th>
                <th className="text-left p-3 font-medium text-sm">Status</th>
                <th className="text-left p-3 font-medium text-sm">Priority</th>
                <th className="text-left p-3 font-medium text-sm">Assignee</th>
                <th className="text-left p-3 font-medium text-sm">Due Date</th>
                <th className="text-left p-3 font-medium text-sm">Progress</th>
                <th className="text-left p-3 font-medium text-sm">Space</th>
              </tr>
            </thead>
            <tbody>
              {tasks.map((task) => (
                <tr key={task.id} className="border-b hover:bg-muted/30">
                  <td className="p-3">
                    <div>
                      <h4 className="font-medium text-sm mb-1">{task.title}</h4>
                      <p className="text-xs text-muted-foreground line-clamp-2 max-w-md">{task.description}</p>
                    </div>
                  </td>
                  <td className="p-3">
                    <div className="flex items-center gap-2">
                      <div className={`w-2 h-2 rounded-full ${getStatusColor(task.status)}`} />
                      <span className="text-sm capitalize">{task.status.replace("-", " ")}</span>
                    </div>
                  </td>
                  <td className="p-3">
                    <Flag className={`h-4 w-4 ${getPriorityColor(task.priority)}`} />
                  </td>
                  <td className="p-3">
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
                  </td>
                  <td className="p-3 text-sm">{task.dueDate}</td>
                  <td className="p-3">
                    <div className="flex items-center gap-2">
                      <Progress value={task.progress} className="h-2 w-20" />
                      <span className="text-xs">{task.progress}%</span>
                    </div>
                  </td>
                  <td className="p-3">
                    <Badge variant="outline" className="text-xs">
                      {task.space}
                    </Badge>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
