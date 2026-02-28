import type { TaskListViewResult, ViewDto } from "../../views-type";
import { StatusSection } from "./status-section";
import { ListTable } from "./list-table";
import { STATUS_CATEGORIES } from "../../../hierarchy/status-constants";
import { Badge } from "@/components/ui/badge";

export function TaskListView({
  data,
  view,
}: {
  data: TaskListViewResult;
  view: ViewDto;
}) {
  const { tasks, statuses } = data;

  const displayConfig = view.displayConfigJson
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

  return (
    <div className="space-y-12 pb-10">
      {!isGroupedByStatus ? (
        <ListTable tasks={tasks} visibleCols={visibleCols} />
      ) : (
        STATUS_CATEGORIES.map((cat) => {
          const catStatuses = statuses.filter((s) => s.category === cat.id);
          if (catStatuses.length === 0) return null;

          const catTasksCount = tasks.filter((t) =>
            catStatuses.some((s) => s.id === t.statusId),
          ).length;

          return (
            <div key={cat.id} className="space-y-4">
              <div className="flex items-center gap-3 px-2">
                <div
                  className={`px-3 py-1 rounded-full text-[10px] font-black uppercase tracking-widest shadow-sm ${cat.bgColor} ${cat.color}`}
                >
                  {cat.label}
                </div>
                <div className="h-px flex-1 bg-muted/20" />
                <Badge
                  variant="ghost"
                  className="text-[10px] text-muted-foreground/40 font-bold uppercase tracking-tighter"
                >
                  {catTasksCount} {catTasksCount === 1 ? "task" : "tasks"}
                </Badge>
              </div>

              <div className="p-4 rounded-2xl border-2 border-dashed border-muted/20 bg-muted/5 space-y-10">
                {catStatuses.map((status) => {
                  const statusTasks = tasks.filter(
                    (t) => t.statusId === status.id,
                  );
                  return (
                    <StatusSection
                      key={status.id}
                      status={status}
                      tasks={statusTasks}
                      visibleCols={visibleCols}
                    />
                  );
                })}
              </div>
            </div>
          );
        })
      )}
    </div>
  );
}
