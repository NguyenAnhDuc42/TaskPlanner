import type {
  TasksBoardViewResult,
  DisplayConfig,
  ViewDto,
  TaskDto,
} from "../../views-type";
import { Plus } from "lucide-react";
import { BoardColumn } from "./board-column";
import { STATUS_CATEGORIES } from "../../../hierarchy/status-constants";
import { type WheelEvent, useMemo, useRef, useState } from "react";
import { TaskDetailSheet } from "../../../tasks/task-detail-sheet";
import { groupStatusesForDisplay } from "../../status-display";

export function TaskBoardView({
  data,
  view,
  workspaceId,
  layerId,
  layerType,
  listId,
}: {
  data: TasksBoardViewResult;
  view: ViewDto;
  workspaceId: string;
  layerId: string;
  layerType: "ProjectSpace" | "ProjectFolder" | "ProjectList";
  listId?: string;
}) {
  const [selectedTask, setSelectedTask] = useState<TaskDto | null>(null);
  const [openInlineColumnId, setOpenInlineColumnId] = useState<string | null>(
    null,
  );
  const { tasks, statuses } = data;
  const groupedStatusDisplay = useMemo(
    () => groupStatusesForDisplay(statuses, tasks),
    [statuses, tasks],
  );
  const groupedStatuses = groupedStatusDisplay.statuses;
  const tasksByStatusId = groupedStatusDisplay.tasksByStatusId;
  const boardScrollRef = useRef<HTMLDivElement | null>(null);

  const handleBoardWheel = (e: WheelEvent<HTMLDivElement>) => {
    // Keep vertical wheel behavior inside column scroll areas.
    const target = e.target as HTMLElement;
    if (target.closest("[data-radix-scroll-area-viewport]")) {
      return;
    }

    const container = boardScrollRef.current;
    if (!container) {
      return;
    }

    if (container.scrollWidth <= container.clientWidth) {
      return;
    }

    if (Math.abs(e.deltaY) <= Math.abs(e.deltaX)) {
      return;
    }

    container.scrollLeft += e.deltaY;
    e.preventDefault();
  };

  const displayConfig: DisplayConfig = view.displayConfigJson
    ? JSON.parse(view.displayConfigJson)
    : { groupBy: "status" };

  const isGroupedByStatus = displayConfig.groupBy === "status";

  if (!isGroupedByStatus) {
    return (
      <div
        ref={boardScrollRef}
        onWheel={handleBoardWheel}
        className="h-full flex gap-4 overflow-x-auto pb-4 no-scrollbar"
      >
        <BoardColumn
          name="All Tasks"
          color="#888888"
          tasks={tasks}
          workspaceId={workspaceId}
          layerId={layerId}
          layerType={layerType}
          listId={listId}
          isInlineOpen={openInlineColumnId === "all"}
          onInlineOpenChange={(open) => {
            setOpenInlineColumnId((current) => {
              if (open) return "all";
              return current === "all" ? null : current;
            });
          }}
          onTaskClick={setSelectedTask}
        />
      </div>
    );
  }

  // Sort statuses by category order first
  const sortedStatuses = [...groupedStatuses].sort((a, b) => {
    const aIndex = STATUS_CATEGORIES.findIndex((cat) => cat.id === a.category);
    const bIndex = STATUS_CATEGORIES.findIndex((cat) => cat.id === b.category);
    return aIndex - bIndex;
  });

  return (
    <div
      ref={boardScrollRef}
      onWheel={handleBoardWheel}
      className="h-full flex gap-8 overflow-x-auto pb-6 no-scrollbar items-start relative"
    >
      <TaskDetailSheet
        task={selectedTask}
        workspaceId={workspaceId}
        isOpen={!!selectedTask}
        onClose={() => setSelectedTask(null)}
      />

      {STATUS_CATEGORIES.map((cat) => {
        const catStatuses = sortedStatuses.filter((s) => s.category === cat.id);
        if (catStatuses.length === 0) return null;

        const catTasksCount = catStatuses.reduce(
          (total, status) => total + (tasksByStatusId[status.id]?.length ?? 0),
          0,
        );

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
                  tasks={tasksByStatusId[s.id] ?? []}
                  statusId={s.id}
                  workspaceId={workspaceId}
                  layerId={layerId}
                  layerType={layerType}
                  listId={listId}
                  isInlineOpen={openInlineColumnId === s.id}
                  onInlineOpenChange={(open) => {
                    setOpenInlineColumnId((current) => {
                      if (open) return s.id;
                      return current === s.id ? null : current;
                    });
                  }}
                  onTaskClick={setSelectedTask}
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
