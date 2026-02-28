import type { TaskListViewResult, ViewDto } from "../../views-type";
import { StatusSection } from "./status-section";
import { ListTable } from "./list-table";

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
    <div className="space-y-8 pb-10">
      {!isGroupedByStatus ? (
        <ListTable tasks={tasks} visibleCols={visibleCols} />
      ) : (
        statuses.map((status) => {
          const statusTasks = tasks.filter((t) => t.statusId === status.id);
          return (
            <StatusSection
              key={status.id}
              status={status}
              tasks={statusTasks}
              visibleCols={visibleCols}
            />
          );
        })
      )}
    </div>
  );
}
