import type { TaskListViewResult, ViewDto, TaskDto } from "../../views-type";
import { StatusSection } from "./status-section";
import { ListTable } from "./list-table";
import { STATUS_CATEGORIES } from "../../../hierarchy/status-constants";
import { useMemo, useState } from "react";
import { TaskDetailSheet } from "../../../tasks/task-detail-sheet";
import { groupStatusesForDisplay } from "../../status-display";

import { EntityLayerType } from "@/types/relationship-type";
import { cn } from "@/lib/utils";

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
  layerType: EntityLayerType;
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
    <div className="space-y-16 pb-20 relative">
      <TaskDetailSheet
        task={selectedTask}
        workspaceId={workspaceId}
        isOpen={!!selectedTask}
        onClose={() => setSelectedTask(null)}
      />

      {!isGroupedByStatus ? (
        <div className="rounded-[2rem] border border-white/5 bg-white/[0.02] backdrop-blur-md overflow-hidden shadow-2xl">
          <ListTable
            tasks={tasks}
            visibleCols={visibleCols}
            onTaskClick={setSelectedTask}
          />
        </div>
      ) : (
        STATUS_CATEGORIES.map((cat) => {
          const catStatuses = groupedStatuses.filter((s) => s.category === cat.id);
          if (catStatuses.length === 0) return null;

          const catTasksCount = catStatuses.reduce(
            (total, status) => total + (tasksByStatusId[status.id]?.length ?? 0),
            0,
          );

          return (
            <div key={cat.id} className="space-y-8">
              {/* Category Header */}
              <div className="flex items-center gap-6 px-2">
                <div className="flex flex-col gap-1">
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
                <div className="h-px flex-1 bg-gradient-to-r from-white/10 to-transparent" />
              </div>

              <div className="space-y-10 group/category">
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
