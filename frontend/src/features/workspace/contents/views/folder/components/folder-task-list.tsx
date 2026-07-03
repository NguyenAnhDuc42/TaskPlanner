import * as React from "react";
import { Plus, Search, X } from "lucide-react";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { CreateTaskForm } from "@/features/workspace/components/forms/create-task-form";
import { EntityLayerType } from "@/types/entity-layer-type";
import { SortableTaskItem } from "./sortable-task-item";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { TaskFilterPopover } from "./task-filter-popover";
import { useDebounce } from "@/hooks/use-debounce";
import { SortableList } from "@/components/sortable-list";
import type { TaskRecord } from "@/types/projects";
import type { Status } from "@/types/status";
import type { Priority } from "@/types/priority";
import type { TaskMutations } from "@/mutations/task.mutations";
import { toLocalDay } from "@/lib/date-filter";

export interface TaskFilter {
  statusIds?: string[];
  priorities?: Priority[];
  assigneeIds?: string[];
  startDate?: string;
  dueDate?: string;
  search?: string;
}

type FolderTaskListProps = Readonly<{
  folderId: string;
  tasks: TaskRecord[];
  taskStatuses: Status[];
  spaceId?: string;
  taskMutations: TaskMutations;
  scheduleFlush: () => void;
  onSelectTask?: (taskId: string) => void;
  selectedTaskId?: string;
  checkedTaskIds?: Set<string>;
  onToggleCheck?: (taskId: string, e: React.MouseEvent) => void;
}>;

export function FolderTaskList({
  folderId,
  tasks: allTasks,
  taskStatuses,
  spaceId,
  taskMutations,
  scheduleFlush,
  onSelectTask,
  selectedTaskId = "1",
  checkedTaskIds = new Set(),
  onToggleCheck,
}: FolderTaskListProps) {
  const { canCreateContent } = useWorkspaceRole();
  const [createOpen, setCreateOpen] = React.useState(false);

  const [filterState, setFilterState] = React.useState<TaskFilter>({});
  const [searchInput, setSearchInput] = React.useState("");
  const debouncedSearch = useDebounce(searchInput, 300);

  const filter: TaskFilter = React.useMemo(() => ({
    ...filterState,
    search: debouncedSearch || undefined
  }), [filterState, debouncedSearch]);

  const setFilter = setFilterState;

  // Tasks are already fully hydrated locally (Bootstrap + Delta) — no pagination/fetch needed,
  // just filter/sort what's already in the store.
  const tasks = React.useMemo(
    () => allTasks
      .filter((t) => {
        if (filter.statusIds?.length) {
          const taskStatusId = t.statusId?.toLowerCase() ?? "";
          if (!filter.statusIds.some((id) => id.toLowerCase() === taskStatusId)) return false;
        }
        if (filter.priorities?.length && !filter.priorities.includes(t.priority ?? "" as Priority)) return false;
        if (debouncedSearch && !t.name.toLowerCase().includes(debouncedSearch.toLowerCase())) return false;
        if (filter.startDate) {
          const day = toLocalDay(t.startDate);
          if (!day || day < toLocalDay(filter.startDate)!) return false;
        }
        if (filter.dueDate) {
          const day = toLocalDay(t.dueDate);
          if (!day || day > toLocalDay(filter.dueDate)!) return false;
        }
        return true;
      })
      .sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1)),
    [allTasks, filter, debouncedSearch],
  );

  const onUpdateTask = React.useCallback((taskId: string, patch: Partial<TaskRecord>) => {
    taskMutations.updateLocal(taskId, patch).catch((err) => console.error("Failed to apply local task update", err));
    scheduleFlush();
  }, [taskMutations, scheduleFlush]);

  const handleReorder = React.useCallback((newTasks: TaskRecord[]) => {
    Promise.all(
      newTasks.map((t, idx) => taskMutations.updateLocal(t.id, { orderKey: String(idx).padStart(6, "0") })),
    )
      .then(() => scheduleFlush())
      .catch((err) => console.error("Failed to reorder tasks", err));
  }, [taskMutations, scheduleFlush]);

  return (
    <div className="flex flex-col h-full w-full bg-transparent relative">

      {/* Search / Filter Bar */}
      <div className="px-1.5 py-1 border-b border-border/15 shrink-0 flex items-center gap-1">
        <div className="flex items-center gap-2 px-2 h-7 rounded-md bg-secondary/60 border border-transparent focus-within:border-primary/30 focus-within:bg-secondary transition-all group flex-1 shadow-inner">
          <Search className="h-3 w-3 text-muted-foreground/40 group-focus-within:text-primary transition-colors shrink-0" />
          <input
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder="Search tasks..."
            className="flex-1 bg-transparent border-none outline-none text-[11px] font-medium text-foreground placeholder:text-muted-foreground/40 transition-all min-w-0"
          />
          {searchInput && (
            <button
              onClick={() => setSearchInput("")}
              className="text-muted-foreground/40 hover:text-foreground transition-colors shrink-0"
            >
              <X className="h-3 w-3" />
            </button>
          )}
        </div>
        <TaskFilterPopover
          filter={filter}
          onChange={setFilter}
          statuses={taskStatuses}
        />
        <div className="w-[1px] h-3.5 bg-border shrink-0" />
        <button
          className="h-7 w-7 flex items-center justify-center rounded-md bg-primary text-primary-foreground hover:bg-primary/90 transition-colors shrink-0 shadow-sm"
          onClick={() => setCreateOpen(true)}
        >
          <Plus className="h-4 w-4" />
        </button>
      </div>

      {/* Task List */}
      <div className="flex-1 overflow-y-auto p-1 space-y-1 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/10 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/30 [&::-webkit-scrollbar-track]:bg-transparent">
        {tasks.length === 0 && (
          <div className="p-4 text-xs text-muted-foreground text-center">No tasks in this folder.</div>
        )}

        <SortableList
          items={tasks}
          onReorder={handleReorder}
          direction="vertical"
          className="space-y-1"
          renderOverlay={draggedId => {
            const t = tasks.find(t => t.id === draggedId);
            return t ? (
              <div className="rotate-1 scale-[1.02] opacity-95 cursor-grabbing pointer-events-none shadow-2xl shadow-black/60">
                <SortableTaskItem task={t} isSelected={false} isChecked={false} onSelect={() => {}} statuses={taskStatuses} spaceId={spaceId} onUpdateTask={onUpdateTask} />
              </div>
            ) : null;
          }}
        >
          {tasks.map((task) => (
            <SortableTaskItem
              key={task.id}
              task={task}
              isSelected={selectedTaskId === task.id}
              isChecked={checkedTaskIds.has(task.id)}
              onSelect={() => onSelectTask?.(task.id)}
              onToggleCheck={onToggleCheck}
              statuses={taskStatuses}
              spaceId={spaceId}
              onUpdateTask={onUpdateTask}
            />
          ))}
        </SortableList>


        {/* Create Task — members and above only */}
        {canCreateContent && <DialogFormWrapper
          title="Create New Task"
          open={createOpen}
          onOpenChange={setCreateOpen}
          trigger={
            <button
              onClick={() => setCreateOpen(true)}
              className="w-full flex items-center justify-center py-2 px-3 rounded-md bg-muted/40 hover:bg-muted/70 text-foreground transition-all border border-border/40 hover:border-border/80 mt-2 gap-1.5 cursor-pointer shadow-sm active:scale-[0.98]"
            >
              <Plus className="h-3.5 w-3.5 text-muted-foreground" />
              <span className="text-[11px] font-semibold text-muted-foreground">Create Task</span>
            </button>
          }
        >
          <CreateTaskForm
            parentId={folderId}
            parentType={EntityLayerType.ProjectFolder}
            onCancel={() => setCreateOpen(false)}
          />
        </DialogFormWrapper>}
      </div>
    </div>
  );
}
