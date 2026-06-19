import * as React from "react";
import { cn } from "@/lib/utils";
import { Check, FileText, MoreVertical } from "lucide-react";
import { StatusBadge } from "@/components/status-badge";
import { PriorityBadge } from "@/components/priority-badge";
import { DateSelect } from "@/components/date-select";
import { DynamicIcon } from "@/components/dynamic-icon";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import type { TaskRecord } from "@/types/projects";
import { useSelector } from "react-redux";
import { statusSelectors, folderSelectors } from "@/store/entityStore";
import type { RootState } from "@/store";
import { StatusSelect } from "@/components/status-select";
import { PrioritySelect } from "@/components/priority-select";
import { useParams } from "@tanstack/react-router";
import { useGetFolderDetailQuery, useBatchUpdateFolderTasks } from "../folder-api";

export interface SortableTaskItemProps {
  task: TaskRecord;
  isSelected: boolean;
  isChecked: boolean;
  onSelect: () => void;
  onToggleCheck?: (taskId: string, e: React.MouseEvent) => void;
}

export function SortableTaskItem({ 
  task, 
  isSelected, 
  isChecked, 
  onSelect, 
  onToggleCheck,
}: Readonly<SortableTaskItemProps>) {
  const statuses = useSelector(statusSelectors.selectAll);
  const { folderId } = useParams({ strict: false }) as { folderId: string };
  useGetFolderDetailQuery(folderId);
  const { mutate: batchUpdate } = useBatchUpdateFolderTasks(folderId);

  const folder = useSelector((state: RootState) => folderSelectors.selectById(state, folderId));

  const taskStatuses = React.useMemo(() => {
    const targetWorkflowId = folder?.workflowId;
    if (!targetWorkflowId) return [];
    return statuses
      .filter(s => s.workflowId?.toLowerCase() === targetWorkflowId.toLowerCase())
      .sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1));
  }, [folder?.workflowId, statuses]);

  const onUpdateTaskField = (fields: Partial<TaskRecord> & { clearStartDate?: boolean; clearDueDate?: boolean }) => {
    batchUpdate([{ id: task.id, ...fields }]);
  };

  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: task.id });

  const style = {
    transform: isDragging ? undefined : CSS.Transform.toString(transform),
    transition: isDragging ? undefined : transition,
    opacity: isDragging ? 0 : 1,
    pointerEvents: isDragging ? "none" as const : undefined,
  };

  const itemColor = task.color || "#3b82f6";
  
  return (
    <div
      ref={setNodeRef}
      style={{ ...style, borderWidth: "1px" }}
      {...attributes}
      {...listeners}
      onClick={onSelect}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          onSelect();
        }
      }}
      className={cn(
        "w-full text-left flex items-start px-2.5 py-1.5 rounded-md transition-all group relative border shadow-sm outline-none cursor-pointer",
        isSelected
          ? "bg-primary/5 border-primary/30" 
          : "bg-muted/40 border-border/50 hover:bg-muted/60"
      )}
    >
      <div
        className={cn(
          "shrink-0 mt-0.5 cursor-pointer z-10 transition-all duration-200 flex items-center justify-center",
          isChecked 
            ? "w-3.5 mr-2 opacity-100" 
            : "w-0 mr-0 opacity-0 group-hover:w-3.5 group-hover:mr-2 group-hover:opacity-100 overflow-hidden"
        )}
        onClick={(e) => {
          e.stopPropagation();
          onToggleCheck?.(task.id, e);
        }}
      >
        <div className={cn(
          "h-3.5 w-3.5 rounded-[3px] border flex items-center justify-center transition-colors shrink-0",
          isChecked
            ? "bg-primary border-primary"
            : "border-muted-foreground/30 group-hover:border-primary bg-background"
        )}>
          {isChecked && <Check className="h-2.5 w-2.5 text-primary-foreground" />}
        </div>
      </div>

      {/* Task Content Column */}
      <div className="flex flex-col gap-1 flex-1 min-w-0">

        {/* Row 1: Name + Menu */}
        <div className="flex items-center justify-between w-full gap-2">
          <div className="flex items-center gap-1.5 min-w-0">
            {task.icon ? (
              <DynamicIcon name={task.icon} className="h-3.5 w-3.5 shrink-0" color={itemColor} />
            ) : (
              <FileText className="h-3.5 w-3.5 opacity-50 shrink-0" style={{ color: itemColor }} />
            )}
            <span className={cn(
              "text-[12px] font-medium truncate",
              isSelected ? "text-primary font-bold" : "text-foreground"
            )}>
              {task.name}
            </span>
          </div>
          <div className="w-4 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity text-muted-foreground shrink-0 cursor-grab active:cursor-grabbing">
            <MoreVertical className="h-3.5 w-3.5" />
          </div>
        </div>

        {/* Row 2: Status + Priority */}
        <div className="flex items-center gap-3">
          <div 
            onClick={(e) => e.stopPropagation()} 
            onPointerDown={(e) => e.stopPropagation()}
          >
            <StatusSelect
              value={task.statusId || undefined}
              onChange={(statusId) => onUpdateTaskField({ statusId })}
              workflowId={taskStatuses[0]?.workflowId}
              statuses={taskStatuses}
              trigger={
                <button type="button" className="cursor-pointer focus:outline-none">
                  <StatusBadge status={statuses.find(s => s.id?.toLowerCase() === task.statusId?.toLowerCase())} />
                </button>
              }
            />
          </div>

          <div 
            onClick={(e) => e.stopPropagation()} 
            onPointerDown={(e) => e.stopPropagation()}
          >
            <PrioritySelect
              value={task.priority}
              onChange={(priority) => onUpdateTaskField({ priority })}
              trigger={
                <button type="button" className="cursor-pointer focus:outline-none">
                  <PriorityBadge priority={task.priority} />
                </button>
              }
            />
          </div>
        </div>

        {/* Row 3: Dates */}
        <div className="flex items-center justify-between w-full mt-0.5">
          <div className="flex items-center gap-2">
            <DateSelect
              startDate={task.startDate}
              dueDate={task.dueDate}
              onStartDateChange={(date) => onUpdateTaskField(date ? { startDate: date.toISOString() } : { clearStartDate: true })}
              onDueDateChange={(date) => onUpdateTaskField(date ? { dueDate: date.toISOString() } : { clearDueDate: true })}
              onClearDates={() => onUpdateTaskField({ clearStartDate: true, clearDueDate: true })}
              size="sm"
              triggerClassName="h-5 px-1.5 text-[9px] font-bold border border-border/5 rounded-sm"
            />
          </div>
        </div>
      </div>
    </div>
  );
}
