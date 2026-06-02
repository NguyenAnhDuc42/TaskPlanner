import * as React from "react";
import { Filter, Plus } from "lucide-react";
import { useParams } from "@tanstack/react-router";
import { useGetFolderTasksQuery, useFolderTasksList, useBatchUpdateFolderTasksMutation } from "../folder-api";
import { Dialog, DialogContent, DialogTrigger } from "@/components/ui/dialog";
import { CreateTaskForm } from "@/features/workspace/components/forms/create-task-form";
import { EntityLayerType } from "@/types/entity-layer-type";
import { SortableTaskItem } from "./sortable-task-item";
import { useDispatch } from "react-redux";
import { taskSlice } from "@/store/entityStore";
import { createPortal } from "react-dom";
import {
  DndContext,
  closestCenter,
  DragOverlay,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from "@dnd-kit/core";
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { restrictToVerticalAxis } from "@dnd-kit/modifiers";
import type { TaskRecord } from "@/types/projects";

type FolderTaskListProps = Readonly<{
  onSelectTask?: (taskId: string) => void;
  selectedTaskId?: string;
  checkedTaskIds?: Set<string>;
  onToggleCheck?: (taskId: string, e: React.MouseEvent) => void;
}>;

export function FolderTaskList({
  onSelectTask,
  selectedTaskId = "1",
  checkedTaskIds = new Set(),
  onToggleCheck,
}: FolderTaskListProps) {
  const { folderId } = useParams({ strict: false }) as { folderId: string };
  const dispatch = useDispatch();
  const [createOpen, setCreateOpen] = React.useState(false);
  const { isLoading } = useGetFolderTasksQuery({ folderId, cursor: null });
  const tasks = useFolderTasksList(folderId);
  const sortableItems = React.useMemo(() => tasks.map(t => t.id), [tasks]);
  const [batchUpdate] = useBatchUpdateFolderTasksMutation();
  const [draggedTask, setDraggedTask] = React.useState<TaskRecord | null>(null);
  const pointerSensor = useSensor(PointerSensor, { activationConstraint: { distance: 5 } });
  const keyboardSensor = useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates });
  const sensors = useSensors(pointerSensor, keyboardSensor);

  function handleDragStart(event: DragStartEvent) {
    const task = tasks.find(t => t.id === event.active.id);
    if (task) setDraggedTask(task);
  }

  function handleDragEnd(event: DragEndEvent) {
    setDraggedTask(null);
    const { active, over } = event;
    if (!over || active.id === over.id) return;

    const oldIndex = tasks.findIndex(t => t.id === active.id);
    const newIndex = tasks.findIndex(t => t.id === over.id);
    const newOrder = arrayMove(tasks, oldIndex, newIndex);

    const updates = newOrder.map((t, idx) => ({
      id: t.id,
      orderKey: String(idx).padStart(6, "0"),
    }));

    // Optimistic update
    dispatch(taskSlice.actions.upsertMany(updates));

    // Backend sync (rollback handled inside mutation lifecycle)
    batchUpdate({ folderId, updates });
  }

  return (
    <div className="flex flex-col h-full w-full bg-transparent relative">

      {/* Search / Filter Bar */}
      <div className="px-1.5 py-1 border-b border-border/15 shrink-0 flex items-center gap-1">
        <div className="h-7 flex-1 bg-muted/30 rounded-md border border-border/30 flex items-center px-2.5 text-[11px] text-muted-foreground/50">
          Search tasks...
        </div>
        <button className="h-7 w-7 flex items-center justify-center rounded-md hover:bg-muted/50 text-muted-foreground transition-colors shrink-0">
          <Filter className="h-3.5 w-3.5" />
        </button>
        <div className="w-[1px] h-3.5 bg-border shrink-0" />
        <button className="h-7 w-7 flex items-center justify-center rounded-md bg-primary text-primary-foreground hover:bg-primary/90 transition-colors shrink-0 shadow-sm">
          <Plus className="h-4 w-4" />
        </button>
      </div>

      {/* Task List */}
      <div className="flex-1 overflow-y-auto p-1 space-y-1 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/10 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/30 [&::-webkit-scrollbar-track]:bg-transparent">
        {isLoading && tasks.length === 0 && (
          <div className="p-4 text-xs text-muted-foreground text-center">Loading tasks...</div>
        )}
        {!isLoading && tasks.length === 0 && (
          <div className="p-4 text-xs text-muted-foreground text-center">No tasks in this folder.</div>
        )}

        <DndContext
          sensors={sensors}
          collisionDetection={closestCenter}
          modifiers={[restrictToVerticalAxis]}
          onDragStart={handleDragStart}
          onDragEnd={handleDragEnd}
        >
          <SortableContext items={sortableItems} strategy={verticalListSortingStrategy}>
            {tasks.map((task) => (
              <SortableTaskItem
                key={task.id}
                task={task}
                isSelected={selectedTaskId === task.id}
                isChecked={checkedTaskIds.has(task.id)}
                onSelect={() => onSelectTask?.(task.id)}
                onToggleCheck={onToggleCheck}
              />
            ))}
          </SortableContext>

          {createPortal(
            <DragOverlay dropAnimation={null}>
              {draggedTask ? (
                <div className="rotate-1 scale-[1.02] opacity-95 cursor-grabbing pointer-events-none shadow-2xl shadow-black/60">
                  <SortableTaskItem
                    task={draggedTask}
                    isSelected={false}
                    isChecked={false}
                    onSelect={() => {}}
                  />
                </div>
              ) : null}
            </DragOverlay>,
            document.body
          )}
        </DndContext>

        {/* Create Task */}
        <Dialog open={createOpen} onOpenChange={setCreateOpen}>
          <DialogTrigger asChild>
            <button className="w-full flex items-center justify-center py-2 px-3 rounded-md bg-muted/40 hover:bg-muted/70 text-foreground transition-all border border-border/40 hover:border-border/80 mt-2 gap-1.5 cursor-pointer shadow-sm active:scale-[0.98]">
              <Plus className="h-3.5 w-3.5 text-muted-foreground" />
              <span className="text-[11px] font-semibold text-muted-foreground">Create Task</span>
            </button>
          </DialogTrigger>
          <DialogContent className="max-w-2xl p-0" showCloseButton={false}>
            <CreateTaskForm
              parentId={folderId}
              parentType={EntityLayerType.ProjectFolder}
              onSuccess={() => setCreateOpen(false)}
              onCancel={() => setCreateOpen(false)}
            />
          </DialogContent>
        </Dialog>
      </div>
    </div>
  );
}
