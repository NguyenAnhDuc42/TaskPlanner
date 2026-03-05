import type { TaskListViewResult, ViewDto, TaskDto } from "../../views-type";
import { StatusSection } from "./status-section";
import { ListTable } from "./list-table";
import { STATUS_CATEGORIES } from "../../../hierarchy/status-constants";
import { useMemo, useState } from "react";
import { TaskDetailSheet } from "../../../tasks/task-detail-sheet";
import { groupStatusesForDisplay } from "../../status-display";

export function TaskListView({
  data,
  view,
  workspaceId,
  layerId,
  layerType,
  listId,
}: {
  data: TaskListViewResult;
  view: ViewDto;
  workspaceId: string;
  layerId: string;
  layerType: "ProjectSpace" | "ProjectFolder" | "ProjectList";
  listId?: string;
}) {
  const [selectedTask, setSelectedTask] = useState<TaskDto | null>(null);
  const [openInlineStatusId, setOpenInlineStatusId] = useState<string | null>(
    null,
  );
  const { tasks, statuses } = data;
  const groupedStatusDisplay = useMemo(
    () => groupStatusesForDisplay(statuses, tasks),
    [statuses, tasks],
  );
  const groupedStatuses = groupedStatusDisplay.statuses;
  const tasksByStatusId = groupedStatusDisplay.tasksByStatusId;

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
    <div className="space-y-12 pb-10 relative">
      <TaskDetailSheet
        task={selectedTask}
        workspaceId={workspaceId}
        isOpen={!!selectedTask}
        onClose={() => setSelectedTask(null)}
      />

      {!isGroupedByStatus ? (
        <ListTable
          tasks={tasks}
          visibleCols={visibleCols}
          onTaskClick={setSelectedTask}
        />
      ) : (
        STATUS_CATEGORIES.map((cat) => {
          const catStatuses = groupedStatuses.filter((s) => s.category === cat.id);
          if (catStatuses.length === 0) return null;

          const catTasksCount = catStatuses.reduce(
            (total, status) => total + (tasksByStatusId[status.id]?.length ?? 0),
            0,
          );

          return (
            <div key={cat.id} className="space-y-4">
              <div className="flex items-center gap-3 px-2">
                <div
                  className={`px-3 py-1 rounded-full text-[10px] font-black uppercase tracking-widest shadow-sm ${cat.bgColor} ${cat.color}`}
                >
                  {cat.label}
                </div>
                <div className="h-px w-8 bg-muted/20" />
                <div className="text-[10px] text-muted-foreground/40 font-bold uppercase tracking-tighter">
                  {catTasksCount} tasks
                </div>
              </div>

              <div className="space-y-6 p-4 rounded-[2rem] border-2 border-dashed border-muted/20 bg-muted/5">
                {catStatuses.map((s) => (
                  <StatusSection
                    key={s.id}
                    status={s}
                    tasks={tasksByStatusId[s.id] ?? []}
                    visibleCols={visibleCols}
                    workspaceId={workspaceId}
                    layerId={layerId}
                    layerType={layerType}
                    listId={listId}
                    isInlineOpen={openInlineStatusId === s.id}
                    onInlineOpenChange={(open) => {
                      setOpenInlineStatusId((current) => {
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
        })
      )}
    </div>
  );
}
