import type {
  TasksBoardViewResult,
  DisplayConfig,
  ViewDto,
} from "../../views-type";
import { Plus } from "lucide-react";
import { BoardColumn } from "./board-column";
import { STATUS_CATEGORIES } from "../../../hierarchy/status-constants";

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

  if (!isGroupedByStatus) {
    return (
      <div className="h-full flex gap-4 overflow-x-auto pb-4 no-scrollbar">
        <BoardColumn name="All Tasks" color="#888888" tasks={tasks} />
      </div>
    );
  }

  // Sort statuses by category order first
  const sortedStatuses = [...statuses].sort((a, b) => {
    const aIndex = STATUS_CATEGORIES.findIndex((cat) => cat.id === a.category);
    const bIndex = STATUS_CATEGORIES.findIndex((cat) => cat.id === b.category);
    return aIndex - bIndex;
  });

  return (
    <div className="h-full flex gap-8 overflow-x-auto pb-6 no-scrollbar items-start">
      {STATUS_CATEGORIES.map((cat) => {
        const catStatuses = sortedStatuses.filter((s) => s.category === cat.id);
        if (catStatuses.length === 0) return null;

        const catTasksCount = tasks.filter((t) =>
          catStatuses.some((s) => s.id === t.statusId),
        ).length;

        return (
          <div key={cat.id} className="flex flex-col gap-4 h-full">
            <div className="flex items-center gap-3 px-2 flex-shrink-0">
              <div
                className={`px-3 py-1 rounded-full text-[10px] font-black uppercase tracking-widest shadow-sm ${cat.bgColor} ${cat.color}`}
              >
                {cat.label}
              </div>
              <div className="h-px w-8 bg-muted/20" />
              <div className="text-[10px] text-muted-foreground/40 font-bold uppercase tracking-tighter">
                {catTasksCount}
              </div>
            </div>

            <div className="flex gap-4 p-4 rounded-[2rem] border-2 border-dashed border-muted/20 bg-muted/5 h-full">
              {catStatuses.map((s) => (
                <BoardColumn
                  key={s.id}
                  name={s.name}
                  color={s.color}
                  tasks={tasks.filter((t) => t.statusId === s.id)}
                />
              ))}
            </div>
          </div>
        );
      })}

      {/* Quick Status Add Column */}
      <div className="w-80 flex-shrink-0 border-2 border-dashed border-muted/20 rounded-2xl flex items-center justify-center bg-muted/5 hover:bg-muted/10 transition-colors cursor-pointer group h-[200px] mt-[44px]">
        <div className="flex items-center gap-2 text-muted-foreground group-hover:text-foreground">
          <Plus className="h-4 w-4" />
          <span className="text-sm font-bold uppercase tracking-wider">
            Add Status
          </span>
        </div>
      </div>
    </div>
  );
}
