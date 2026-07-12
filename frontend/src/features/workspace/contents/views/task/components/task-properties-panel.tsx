import { StatusSelect } from "@/components/status-select";
import { PrioritySelect } from "@/components/priority-select";
import { PriorityBadge } from "@/components/priority-badge";
import { DateSelect } from "@/components/date-select";
import { TaskAssignees } from "../task-components/task-assignees";
import { ChangesFeed } from "@/features/workspace/components/changes-feed";
import { useEntityChanges } from "@/features/workspace/components/use-entity-changes";
import { ExpandableSection } from "@/components/expandable-section";
import type { TaskRecord } from "@/types/projects/task-record";
import type { Priority } from "@/types/priority";

interface TaskPropertiesPanelProps {
  task: TaskRecord;
  onStatusChange: (statusId: string | null) => void;
  onPriorityChange: (priority: Priority) => void;
  onStartDateChange: (date: Date | undefined) => void;
  onDueDateChange: (date: Date | undefined) => void;
  onClearDates: () => void;
}

function PropertyRow({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex items-center justify-between gap-2 h-7">
      <span className="text-[10px] font-semibold text-muted-foreground/50">{label}</span>
      <div className="flex items-center">{children}</div>
    </div>
  );
}

export function TaskPropertiesPanel({
  task,
  onStatusChange,
  onPriorityChange,
  onStartDateChange,
  onDueDateChange,
  onClearDates,
}: Readonly<TaskPropertiesPanelProps>) {
  const { entries: changes, isLoading: isChangesLoading } = useEntityChanges(task.id, "Task");

  return (
    <div className="flex flex-col flex-1 min-h-0 overflow-y-auto px-3 pb-3 [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
      <ExpandableSection title="Properties" defaultOpen>
        <div className="flex flex-col divide-y divide-border/20">
          <PropertyRow label="Status">
            <StatusSelect value={task.statusId ?? undefined} onChange={onStatusChange} spaceId={task.spaceId!} align="end" />
          </PropertyRow>
          <PropertyRow label="Priority">
            <PrioritySelect
              value={task.priority}
              onChange={onPriorityChange}
              align="end"
              trigger={
                <button type="button" className="cursor-pointer focus:outline-none bg-transparent border-none p-0">
                  <PriorityBadge priority={task.priority} />
                </button>
              }
            />
          </PropertyRow>
          <PropertyRow label="Dates">
            <DateSelect
              startDate={task.startDate}
              dueDate={task.dueDate}
              onStartDateChange={onStartDateChange}
              onDueDateChange={onDueDateChange}
              onClearDates={onClearDates}
              size="sm"
            />
          </PropertyRow>
        </div>
      </ExpandableSection>

      <ExpandableSection title="Assignees" defaultOpen>
        <TaskAssignees taskId={task.id} variant="list" />
      </ExpandableSection>

      <ExpandableSection title="Changes">
        <ChangesFeed entries={changes} isLoading={isChangesLoading} />
      </ExpandableSection>
    </div>
  );
}
