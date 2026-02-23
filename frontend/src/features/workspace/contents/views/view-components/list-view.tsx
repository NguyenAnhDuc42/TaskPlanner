import type { ViewResponse } from "../views-type";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";

export function TaskListView({ data }: { data: ViewResponse }) {
  const { tasks, statuses } = data;

  return (
    <div className="space-y-8">
      {statuses.map((status) => {
        const statusTasks = tasks.filter((t) => t.status_id === status.id);

        return (
          <div key={status.id} className="space-y-4">
            <div className="flex items-center gap-2">
              <div
                className="w-2 h-2 rounded-full"
                style={{ backgroundColor: status.color }}
              />
              <h3 className="font-semibold text-sm uppercase tracking-wider text-muted-foreground">
                {status.name}
              </h3>
              <Badge
                variant="secondary"
                className="ml-1 text-[10px] px-1.5 py-0"
              >
                {statusTasks.length}
              </Badge>
            </div>

            <div className="border rounded-md bg-background overflow-hidden">
              <Table>
                <TableHeader className="bg-muted/30">
                  <TableRow>
                    <TableHead className="w-[400px]">Task Name</TableHead>
                    <TableHead>Assignee</TableHead>
                    <TableHead>Due Date</TableHead>
                    <TableHead className="text-right">Priority</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {statusTasks.map((task) => (
                    <TableRow
                      key={task.id}
                      className="hover:bg-muted/20 cursor-pointer"
                    >
                      <TableCell className="font-medium underline-offset-4 hover:underline">
                        {task.name}
                      </TableCell>
                      <TableCell>
                        <div className="flex -space-x-1">
                          {/* Assignee avatars here */}
                          <div className="h-6 w-6 rounded-full bg-accent border flex items-center justify-center text-[10px]">
                            ?
                          </div>
                        </div>
                      </TableCell>
                      <TableCell className="text-muted-foreground text-sm">
                        {task.due_date
                          ? new Date(task.due_date).toLocaleDateString()
                          : "-"}
                      </TableCell>
                      <TableCell className="text-right">
                        <Badge variant="outline" className="text-[10px]">
                          Normal
                        </Badge>
                      </TableCell>
                    </TableRow>
                  ))}
                  {statusTasks.length === 0 && (
                    <TableRow>
                      <TableCell
                        colSpan={4}
                        className="text-center py-8 text-muted-foreground italic text-sm"
                      >
                        No tasks in this status.
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </div>
          </div>
        );
      })}
    </div>
  );
}
