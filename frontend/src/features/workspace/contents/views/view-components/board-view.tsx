import type { ViewResponse } from "../views-type";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Plus, MoreHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";

export function TaskBoardView({ data }: { data: ViewResponse }) {
  const { tasks, statuses } = data;

  return (
    <div className="h-full flex gap-4 overflow-x-auto pb-4 no-scrollbar">
      {statuses.map((status) => {
        const statusTasks = tasks.filter((t) => t.status_id === status.id);

        return (
          <div
            key={status.id}
            className="w-80 flex-shrink-0 flex flex-col h-full bg-muted/20 rounded-lg border shadow-sm"
          >
            <div className="p-3 border-b bg-background/50 flex items-center justify-between rounded-t-lg">
              <div className="flex items-center gap-2 overflow-hidden">
                <div
                  className="w-3 h-3 rounded-full flex-shrink-0"
                  style={{ backgroundColor: status.color }}
                />
                <h3 className="font-semibold text-sm truncate uppercase tracking-tight">
                  {status.name}
                </h3>
                <Badge variant="secondary" className="px-1.5 h-4 text-[10px]">
                  {statusTasks.length}
                </Badge>
              </div>
              <Button variant="ghost" size="icon" className="h-6 w-6">
                <MoreHorizontal className="h-4 w-4 text-muted-foreground" />
              </Button>
            </div>

            <ScrollArea className="flex-1 p-2">
              <div className="space-y-3">
                {statusTasks.map((task) => (
                  <Card
                    key={task.id}
                    className="shadow-none hover:shadow-md transition-shadow cursor-grab active:cursor-grabbing border-muted-foreground/10 group"
                  >
                    <CardHeader className="p-3 pb-2 space-y-2">
                      <div className="flex justify-between items-start gap-2">
                        <CardTitle className="text-sm font-medium leading-tight group-hover:text-primary transition-colors">
                          {task.name}
                        </CardTitle>
                      </div>
                    </CardHeader>
                    <CardContent className="p-3 pt-0">
                      <div className="flex items-center justify-between mt-2">
                        <div className="flex -space-x-1">
                          <div className="h-5 w-5 rounded-full bg-accent border flex items-center justify-center text-[8px]">
                            ?
                          </div>
                        </div>
                        {task.due_date && (
                          <span className="text-[10px] text-muted-foreground">
                            {new Date(task.due_date).toLocaleDateString(
                              undefined,
                              { month: "short", day: "numeric" },
                            )}
                          </span>
                        )}
                      </div>
                    </CardContent>
                  </Card>
                ))}

                <Button
                  variant="ghost"
                  className="w-full justify-start gap-2 h-9 text-muted-foreground hover:text-foreground hover:bg-background/80 shadow-sm border border-dashed text-xs"
                >
                  <Plus className="h-3 w-3" />
                  Add Task
                </Button>
              </div>
            </ScrollArea>
          </div>
        );
      })}

      {/* Quick Status Add Column */}
      <div className="w-80 flex-shrink-0 border border-dashed rounded-lg flex items-center justify-center bg-muted/5 hover:bg-muted/10 transition-colors cursor-pointer group h-fit py-4">
        <div className="flex items-center gap-2 text-muted-foreground group-hover:text-foreground">
          <Plus className="h-4 w-4" />
          <span className="text-sm font-medium">Add Status</span>
        </div>
      </div>
    </div>
  );
}
