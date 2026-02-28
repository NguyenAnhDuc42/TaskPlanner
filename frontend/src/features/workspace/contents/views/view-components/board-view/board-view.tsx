import type {
  TasksBoardViewResult,
  DisplayConfig,
  ViewDto,
} from "../../views-type";
import { Plus } from "lucide-react";
import { BoardColumn } from "./board-column";

export function TaskBoardView({
  data,
  view,
}: {
  data: TasksBoardViewResult;
  view: ViewDto;
}) {
  const { tasks, statuses } = data;

  const displayConfig: DisplayConfig = view.displayConfigJson
    ? JSON.parse(view.displayConfigJson)
    : { groupBy: "status" };

  const isGroupedByStatus = displayConfig.groupBy === "status";

  const columns = isGroupedByStatus
    ? statuses.map((s) => ({
        ...s,
        columnTasks: tasks.filter((t) => t.statusId === s.id),
      }))
    : [{ id: "all", name: "All Tasks", color: "#888888", columnTasks: tasks }];

  return (
    <div className="h-full flex gap-4 overflow-x-auto pb-4 no-scrollbar">
      {columns.map((column) => (
        <BoardColumn
          key={column.id}
          name={column.name}
          color={column.color}
          tasks={column.columnTasks}
        />
      ))}

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
