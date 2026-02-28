import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import type { TaskDto } from "../../views-type";
import { Calendar, User, Flag } from "lucide-react";
import { cn } from "@/lib/utils";

interface ListTableProps {
  tasks: TaskDto[];
  visibleCols: string[];
}

const getPriorityStyles = (priority: string) => {
  switch (priority?.toLowerCase()) {
    case "urgent":
      return "bg-red-500/10 text-red-500 border-red-500/20";
    case "high":
      return "bg-orange-500/10 text-orange-500 border-orange-500/20";
    case "normal":
      return "bg-blue-500/10 text-blue-500 border-blue-500/20";
    case "low":
      return "bg-slate-500/10 text-slate-500 border-slate-500/20";
    default:
      return "bg-slate-500/10 text-slate-500 border-slate-500/20";
  }
};

export function ListTable({ tasks, visibleCols }: ListTableProps) {
  return (
    <div className="rounded-xl border bg-card/30 backdrop-blur-sm overflow-hidden shadow-sm">
      <Table>
        <TableHeader className="bg-muted/40">
          <TableRow className="hover:bg-transparent border-b">
            <TableHead className="w-[450px] font-bold text-xs uppercase tracking-wider h-10">
              Task Name
            </TableHead>
            {visibleCols.includes("assignee") && (
              <TableHead className="font-bold text-xs uppercase tracking-wider h-10">
                Assignee
              </TableHead>
            )}
            {visibleCols.includes("dueDate") && (
              <TableHead className="font-bold text-xs uppercase tracking-wider h-10">
                Due Date
              </TableHead>
            )}
            {visibleCols.includes("priority") && (
              <TableHead className="text-right font-bold text-xs uppercase tracking-wider h-10 pr-6">
                Priority
              </TableHead>
            )}
          </TableRow>
        </TableHeader>
        <TableBody>
          {tasks.map((task) => (
            <TableRow
              key={task.id}
              className="hover:bg-muted/30 cursor-pointer group/row transition-colors border-b last:border-0 h-11"
            >
              <TableCell className="font-medium py-2">
                <div className="flex items-center gap-3">
                  <div className="w-4 h-4 rounded border-2 border-muted-foreground/30 group-hover/row:border-primary/50 transition-colors" />
                  <span className="group-hover/row:text-primary transition-colors text-[13px]">
                    {task.name}
                  </span>
                </div>
              </TableCell>

              {visibleCols.includes("assignee") && (
                <TableCell className="py-2">
                  <div className="flex items-center gap-2">
                    <div className="h-6 w-6 rounded-full bg-muted border flex items-center justify-center overflow-hidden">
                      <User className="h-3 w-3 text-muted-foreground" />
                    </div>
                  </div>
                </TableCell>
              )}

              {visibleCols.includes("dueDate") && (
                <TableCell className="py-2">
                  <div className="flex items-center gap-1.5 text-muted-foreground text-[12px]">
                    <Calendar className="h-3 w-3" />
                    {task.dueDate
                      ? new Date(task.dueDate).toLocaleDateString(undefined, {
                          month: "short",
                          day: "numeric",
                        })
                      : "-"}
                  </div>
                </TableCell>
              )}

              {visibleCols.includes("priority") && (
                <TableCell className="text-right py-2 pr-6">
                  <Badge
                    variant="outline"
                    className={cn(
                      "text-[10px] uppercase font-bold border px-2 py-0 h-5 leading-none",
                      getPriorityStyles(task.priority),
                    )}
                  >
                    <Flag className="h-2.5 w-2.5 mr-1 fill-current" />
                    {task.priority || "Normal"}
                  </Badge>
                </TableCell>
              )}
            </TableRow>
          ))}

          {tasks.length === 0 && (
            <TableRow>
              <TableCell
                colSpan={visibleCols.length + 1}
                className="text-center py-12 text-muted-foreground/60 italic text-sm bg-muted/5 font-medium"
              >
                No tasks in this category.
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </div>
  );
}
