import type { TaskDto, TaskListViewResult } from "../views-type";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import type { DisplayConfig, ViewDto } from "../views-type";

export function TaskListView({
  data,
  view,
}: {
  data: TaskListViewResult;
  view: ViewDto;
}) {
  const { tasks, statuses } = data;

  const displayConfig: DisplayConfig = view.displayConfigJson
    ? JSON.parse(view.displayConfigJson)
    : {
        groupBy: "status",
        visibleColumns: ["assignee", "dueDate", "priority"],
      };

  const isGroupedByStatus = displayConfig.groupBy === "status";
  const visibleCols = displayConfig.visibleColumns || [
    "assignee",
    "dueDate",
    "priority",
  ];

  // Helper to render table
  const renderTable = (taskList: TaskDto[]) => (
    <div className="border rounded-md bg-background overflow-hidden shadow-sm">
      <Table>
        <TableHeader className="bg-muted/30">
          <TableRow>
            <TableHead className="w-[400px]">Task Name</TableHead>
            {visibleCols.includes("assignee") && (
              <TableHead>Assignee</TableHead>
            )}
            {visibleCols.includes("dueDate") && <TableHead>Due Date</TableHead>}
            {visibleCols.includes("priority") && (
              <TableHead className="text-right">Priority</TableHead>
            )}
          </TableRow>
        </TableHeader>
        <TableBody>
          {taskList.map((task) => (
            <TableRow
              key={task.id}
              className="hover:bg-muted/20 cursor-pointer group"
            >
              <TableCell className="font-medium">
                <span className="group-hover:text-primary transition-colors underline-offset-4 group-hover:underline">
                  {task.name}
                </span>
              </TableCell>
              {visibleCols.includes("assignee") && (
                <TableCell>
                  <div className="flex -space-x-1">
                    <div className="h-6 w-6 rounded-full bg-accent border flex items-center justify-center text-[10px] text-muted-foreground">
                      ?
                    </div>
                  </div>
                </TableCell>
              )}
              {visibleCols.includes("dueDate") && (
                <TableCell className="text-muted-foreground text-sm">
                  {task.dueDate
                    ? new Date(task.dueDate).toLocaleDateString()
                    : "-"}
                </TableCell>
              )}
              {visibleCols.includes("priority") && (
                <TableCell className="text-right">
                  <Badge
                    variant="outline"
                    className="text-[10px] uppercase font-normal text-muted-foreground"
                  >
                    {task.priority || "Normal"}
                  </Badge>
                </TableCell>
              )}
            </TableRow>
          ))}
          {taskList.length === 0 && (
            <TableRow>
              <TableCell
                colSpan={visibleCols.length + 1}
                className="text-center py-8 text-muted-foreground italic text-sm"
              >
                No tasks found.
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </div>
  );

  return (
    <div className="space-y-8 pb-10">
      {!isGroupedByStatus
        ? renderTable(tasks)
        : statuses.map((status) => {
            const statusTasks = tasks.filter((t) => t.statusId === status.id);

            return (
              <div key={status.id} className="space-y-3">
                <div className="flex items-center gap-2 px-1">
                  <div
                    className="w-2 h-2 rounded-full"
                    style={{ backgroundColor: status.color }}
                  />
                  <h3 className="font-semibold text-[13px] uppercase tracking-wider text-muted-foreground">
                    {status.name}
                  </h3>
                  <Badge
                    variant="secondary"
                    className="ml-1 text-[10px] px-1.5 py-0 font-medium"
                  >
                    {statusTasks.length}
                  </Badge>
                </div>
                {renderTable(statusTasks)}
              </div>
            );
          })}
    </div>
  );
}
