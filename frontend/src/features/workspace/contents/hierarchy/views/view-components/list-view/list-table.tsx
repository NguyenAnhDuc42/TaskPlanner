import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import type { TaskDto } from "../../views-type";
import { Calendar, User, Flag, Hash } from "lucide-react";

interface ListTableProps {
  tasks: TaskDto[];
  visibleCols: string[];
  onTaskClick?: (task: TaskDto) => void;
}

const getPriorityVisuals = (priority: string) => {
  switch (priority?.toLowerCase()) {
    case "urgent":
      return { color: "#ef4444", label: "URGENT", glow: "rgba(239, 68, 68, 0.2)" };
    case "high":
      return { color: "#f97316", label: "HIGH", glow: "rgba(249, 115, 22, 0.2)" };
    case "normal":
      return { color: "#3b82f6", label: "NORMAL", glow: "rgba(59, 130, 246, 0.2)" };
    case "low":
      return { color: "#64748b", label: "LOW", glow: "rgba(100, 116, 139, 0.2)" };
    default:
      return { color: "#64748b", label: "NORMAL", glow: "rgba(100, 116, 139, 0.2)" };
  }
};

export function ListTable({ tasks, visibleCols, onTaskClick }: ListTableProps) {
  return (
    <div className="overflow-hidden">
      <Table>
        <TableHeader className="bg-white/[0.03] border-b border-white/5">
          <TableRow className="hover:bg-transparent border-none">
            <TableHead className="w-[450px] h-10 px-6">
              <div className="flex items-center gap-2">
                <Hash className="h-3 w-3 text-muted-foreground/30" />
                <span className="text-[9px] font-black uppercase tracking-[0.2em] text-muted-foreground/50">Objective Designation</span>
              </div>
            </TableHead>
            {visibleCols.includes("assignee") && (
              <TableHead className="h-10 px-4">
                <span className="text-[9px] font-black uppercase tracking-[0.2em] text-muted-foreground/50">Operator</span>
              </TableHead>
            )}
            {visibleCols.includes("dueDate") && (
              <TableHead className="h-10 px-4">
                <span className="text-[9px] font-black uppercase tracking-[0.2em] text-muted-foreground/50">Timestamp</span>
              </TableHead>
            )}
            {visibleCols.includes("priority") && (
              <TableHead className="text-right h-10 px-6">
                <span className="text-[9px] font-black uppercase tracking-[0.2em] text-muted-foreground/50">Priority</span>
              </TableHead>
            )}
          </TableRow>
        </TableHeader>
        <TableBody>
          {tasks.map((task) => {
            const prio = getPriorityVisuals(task.priority);
            return (
              <TableRow
                key={task.id}
                onClick={() => onTaskClick?.(task)}
                className="group/row transition-all duration-300 border-b border-white/[0.03] last:border-0 h-12 cursor-pointer hover:bg-white/[0.04]"
              >
                <TableCell className="px-6 py-2">
                  <div className="flex items-center gap-4">
                    <div className="w-4 h-4 rounded-md border border-white/10 group-hover/row:border-primary/50 group-hover/row:shadow-[0_0_8px_var(--primary-foreground)] transition-all bg-black/20" />
                    <span className="text-[13px] font-bold text-foreground/80 group-hover/row:text-foreground transition-colors tracking-tight">
                      {task.name}
                    </span>
                  </div>
                </TableCell>

                {visibleCols.includes("assignee") && (
                  <TableCell className="px-4 py-2">
                    <div className="flex items-center gap-2">
                      <div className="h-7 w-7 rounded-full bg-white/5 border border-white/10 flex items-center justify-center overflow-hidden group-hover/row:border-white/20 transition-all shadow-inner">
                        <User className="h-3.5 w-3.5 text-muted-foreground/40" />
                      </div>
                    </div>
                  </TableCell>
                )}

                {visibleCols.includes("dueDate") && (
                  <TableCell className="px-4 py-2">
                    <div className="flex items-center gap-2 text-[11px] font-bold text-muted-foreground/40 group-hover/row:text-muted-foreground/70 transition-colors uppercase tracking-wider">
                      <Calendar className="h-3.5 w-3.5 opacity-50" />
                      {task.dueDate
                        ? new Date(task.dueDate).toLocaleDateString(undefined, {
                            month: "short",
                            day: "numeric",
                          })
                        : "---"}
                    </div>
                  </TableCell>
                )}

                {visibleCols.includes("priority") && (
                  <TableCell className="text-right px-6 py-2">
                    <div 
                      className="inline-flex items-center gap-2 px-2.5 py-1 rounded-md border transition-all duration-300 bg-white/[0.02]"
                      style={{ 
                        borderColor: `${prio.color}30`,
                        color: prio.color,
                        boxShadow: `inset 0 0 10px ${prio.glow}`
                      }}
                    >
                      <Flag className="h-3 w-3 fill-current opacity-80" />
                      <span className="text-[9px] font-black uppercase tracking-[0.15em]">{prio.label}</span>
                    </div>
                  </TableCell>
                )}
              </TableRow>
            );
          })}

          {tasks.length === 0 && (
            <TableRow className="hover:bg-transparent">
              <TableCell
                colSpan={visibleCols.length + 1}
                className="text-center py-16"
              >
                <div className="flex flex-col items-center gap-2">
                  <div className="text-[10px] font-black text-muted-foreground/20 uppercase tracking-[0.3em]">No objectives found in sector</div>
                  <div className="h-px w-20 bg-white/5" />
                </div>
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </div>
  );
}
