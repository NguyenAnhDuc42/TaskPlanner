import { useEffect } from "react";
import type { TaskStatusWidgetData } from "../../dashboard-type";
import { Badge } from "@/components/ui/badge";
import { Clock, Calendar, ListTodo, AlertCircle } from "lucide-react";

interface TaskListWidgetProps {
  data: TaskStatusWidgetData;
  expanded?: boolean;
}

export function TaskListWidget({ data, expanded = false }: TaskListWidgetProps) {
  useEffect(() => {
    console.log("[TaskListWidget] Data Received:", data);
  }, [data]);

  return (
    <div className={`flex flex-col ${expanded ? 'min-h-0' : 'h-full'} bg-transparent`}>
      {/* Stat Cards Row */}
      <div className="grid grid-cols-3 gap-2 p-3 border-b border-border/10 bg-transparent">
        <div className="flex flex-col items-center justify-center p-2 rounded-lg border border-border/10 bg-[var(--theme-item-normal)] shadow-none">
          <ListTodo className="h-3.5 w-3.5 text-[var(--theme-text-normal)] mb-1" />
          <span className="text-lg font-bold leading-none text-[var(--theme-text-hover)]">{data.totalCount}</span>
          <span className="text-[9px] uppercase font-bold text-[var(--theme-text-normal)] tracking-wider mt-1">Total</span>
        </div>
        <div className="flex flex-col items-center justify-center p-2 rounded-lg border border-orange-500/20 bg-[var(--theme-item-normal)] shadow-none">
          <Calendar className="h-3.5 w-3.5 text-orange-500 mb-1" />
          <span className="text-lg font-bold leading-none text-[var(--theme-text-hover)]">{data.todayCount}</span>
          <span className="text-[9px] uppercase font-bold text-[var(--theme-text-normal)] tracking-wider mt-1">Today</span>
        </div>
        <div className="flex flex-col items-center justify-center p-2 rounded-lg border border-red-500/20 bg-[var(--theme-item-normal)] shadow-none">
          <AlertCircle className="h-3.5 w-3.5 text-red-500 mb-1" />
          <span className="text-lg font-bold leading-none text-[var(--theme-text-hover)]">{data.overdueCount}</span>
          <span className="text-[9px] uppercase font-bold text-[var(--theme-text-normal)] tracking-wider mt-1">Overdue</span>
        </div>
      </div>

      {/* Task List Section */}
      <div className="flex-1 overflow-y-auto p-2 space-y-1">
        {data.tasks.map((task) => (
          <div 
            key={task.id} 
            className="group relative flex flex-col gap-1 p-2.5 rounded-lg border border-transparent hover:border-border/10 hover:bg-[var(--theme-item-hover)] transition-all cursor-pointer"
          >
            <div className="flex items-center justify-between gap-2">
              <span className="text-xs font-semibold truncate leading-tight text-[var(--theme-text-normal)] group-hover:text-[var(--theme-text-hover)] transition-colors">
                {task.title}
              </span>
              <Badge variant="outline" className="text-[9px] h-4 px-1 rounded-sm uppercase font-bold shrink-0 border-border/10 text-[var(--theme-text-normal)]">
                {task.priority !== undefined ? `${task.priority}` : 'No Priority'}
              </Badge>
            </div>
            
            <div className="flex items-center gap-3 text-[10px] text-[var(--theme-text-normal)] font-medium opacity-60">
              <div className="flex items-center gap-1">
                <Clock className="h-2.5 w-2.5" />
                <span>{task.dueDate ? new Date(task.dueDate).toLocaleDateString() : 'No Due Date'}</span>
              </div>
              <div className="w-1 h-1 rounded-full bg-current opacity-30" />
              <span>{task.statusId.slice(0, 8)}...</span>
            </div>
          </div>
        ))}

        {data.tasks.length === 0 && (
          <div className="flex flex-col items-center justify-center h-full text-muted-foreground gap-2 pt-8">
            <ListTodo className="h-8 w-8 opacity-20" />
            <span className="text-xs font-medium">No tasks found in this area.</span>
          </div>
        )}
      </div>
    </div>
  );
}
