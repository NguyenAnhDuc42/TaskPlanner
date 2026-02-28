import type { TaskDto } from "../../views-type";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Calendar, Flag } from "lucide-react";
import { cn } from "@/lib/utils";

interface TaskCardProps {
  task: TaskDto;
}

const getPriorityStyles = (priority: string) => {
  switch (priority?.toLowerCase()) {
    case "urgent":
      return "border-l-red-500 text-red-500";
    case "high":
      return "border-l-orange-500 text-orange-500";
    case "normal":
      return "border-l-blue-500 text-blue-500";
    case "low":
      return "border-l-slate-400 text-slate-400";
    default:
      return "border-l-transparent";
  }
};

export function TaskCard({ task }: TaskCardProps) {
  const priorityStyle = getPriorityStyles(task.priority);

  return (
    <Card
      className={cn(
        "shadow-sm hover:shadow-md transition-all duration-200 cursor-grab active:cursor-grabbing border-muted-foreground/10 group bg-card/50 backdrop-blur-[2px] border-l-4",
        priorityStyle,
      )}
    >
      <CardHeader className="p-3 pb-2 space-y-2">
        <div className="flex justify-between items-start gap-2">
          <CardTitle className="text-[13px] font-semibold leading-relaxed group-hover:text-primary transition-colors line-clamp-2">
            {task.name}
          </CardTitle>
        </div>
      </CardHeader>
      <CardContent className="p-3 pt-0">
        <div className="flex items-center justify-between mt-1">
          <div className="flex items-center gap-2">
            {task.priority && (
              <Flag
                className={cn(
                  "h-3 w-3 fill-current",
                  priorityStyle.split(" ")[1],
                )}
              />
            )}
            <div className="h-5 w-5 rounded-full bg-muted border flex items-center justify-center text-[8px] text-muted-foreground font-bold">
              ?
            </div>
          </div>
          {task.dueDate && (
            <div className="flex items-center gap-1 text-[10px] text-muted-foreground font-medium bg-muted/30 px-1.5 py-0.5 rounded">
              <Calendar className="h-2.5 w-2.5" />
              {new Date(task.dueDate).toLocaleDateString(undefined, {
                month: "short",
                day: "numeric",
              })}
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
