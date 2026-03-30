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
import type { EntityLayerType } from "@/types/relationship-type";
import { cn } from "@/lib/utils";

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
  layerType: EntityLayerType;
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
    const target = e.target as HTMLElement;
    if (target.closest("[data-radix-scroll-area-viewport]")) {
      return;
    }

    const container = boardScrollRef.current;
    if (!container) return;

    if (container.scrollWidth <= container.clientWidth) return;
    if (Math.abs(e.deltaY) <= Math.abs(e.deltaX)) return;

    container.scrollLeft += e.deltaY;
    e.preventDefault();
  };

  const displayConfig: DisplayConfig = view.displayConfigJson
    ? JSON.parse(view.displayConfigJson)
    : { groupBy: "status" };

  const isGroupedByStatus = displayConfig.groupBy === "status";

  // Detail Sheet
  const renderDetailSheet = (
    <TaskDetailSheet
      task={selectedTask}
      workspaceId={workspaceId}
      isOpen={!!selectedTask}
      onClose={() => setSelectedTask(null)}
    />
  );

  if (!isGroupedByStatus) {
    return (
      <div
        ref={boardScrollRef}
        onWheel={handleBoardWheel}
        className="h-full flex gap-6 overflow-x-auto pb-10 no-scrollbar items-start"
      >
        {renderDetailSheet}
        <BoardColumn
          name="Active Objectives"
          color="var(--primary)"
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

  const sortedStatuses = [...groupedStatuses].sort((a, b) => {
    const aIndex = STATUS_CATEGORIES.findIndex((cat) => cat.id === a.category);
    const bIndex = STATUS_CATEGORIES.findIndex((cat) => cat.id === b.category);
    return aIndex - bIndex;
  });

  return (
    <div
      ref={boardScrollRef}
      onWheel={handleBoardWheel}
      className="h-full flex gap-16 overflow-x-auto pb-12 no-scrollbar items-start relative px-2"
    >
      {renderDetailSheet}

      {STATUS_CATEGORIES.map((cat) => {
        const catStatuses = sortedStatuses.filter((s) => s.category === cat.id);
        if (catStatuses.length === 0) return null;

        const catTasksCount = catStatuses.reduce(
          (total, status) => total + (tasksByStatusId[status.id]?.length ?? 0),
          0,
        );

        return (
          <div key={cat.id} className="flex flex-col gap-6 h-full flex-shrink-0">
            {/* Category Header */}
            <div className="flex flex-col gap-1 px-4 flex-shrink-0 relative">
              <div className="flex items-center gap-3">
                <div className={cn("w-2 h-0.5 rounded-full", cat.color.replace('text-', 'bg-'))} />
                <h2 className={cn("text-[11px] font-black uppercase tracking-[0.3em]", cat.color)}>
                  {cat.label}
                </h2>
              </div>
              <div className="text-[9px] font-bold text-muted-foreground/30 uppercase tracking-[0.2em] pl-5">
                Sector Registry: {catTasksCount} Objectives
              </div>
            </div>

            <div className="flex gap-6 h-full items-start">
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

      {/* Add Status Prompt */}
      <div className="w-[340px] flex-shrink-0 rounded-[2.5rem] border border-dashed border-white/5 bg-white/[0.01] flex flex-col items-center justify-center h-[300px] mt-[64px] group cursor-pointer hover:bg-white/[0.03] transition-all duration-500">
        <div className="p-4 rounded-2xl bg-white/5 group-hover:bg-primary/20 transition-all duration-500 group-hover:scale-110 shadow-2xl">
          <Plus className="h-6 w-6 text-muted-foreground/40 group-hover:text-primary transition-colors" />
        </div>
        <span className="mt-4 text-[10px] font-black text-muted-foreground/30 uppercase tracking-[0.3em] group-hover:text-foreground transition-colors">
          Initialize New Sector
        </span>
      </div>
    </div>
  );
}
