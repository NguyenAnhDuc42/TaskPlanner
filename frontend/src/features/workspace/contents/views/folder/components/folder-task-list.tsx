import * as React from "react";
import { Filter, Plus } from "lucide-react";
import { useParams } from "@tanstack/react-router";
import { useGetFolderTasks } from "../folder-api";
import { Dialog, DialogContent, DialogTrigger } from "@/components/ui/dialog";
import { CreateTaskForm } from "@/features/workspace/components/forms/create-task-form";
import { useHierarchyStore } from "@/features/workspace/contents/hierarchy/use-hierarchy-store";
import { EntityLayerType } from "@/types/entity-layer-type";
import { SortableTaskItem } from "./sortable-task-item";
import { 
  DndContext, 
  closestCenter, 
  KeyboardSensor, 
  PointerSensor, 
  useSensor, 
  useSensors,
  type DragEndEvent
} from '@dnd-kit/core';
import { 
  arrayMove, 
  SortableContext, 
  sortableKeyboardCoordinates, 
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import { useBatchUpdateFolderTasks } from "../folder-api";
import type { TaskRecord } from "@/types/projects";
import { useQueryClient } from "@tanstack/react-query";

const EMPTY_ARRAY: string[] = [];

interface FolderTaskListProps {
  onSelectTask?: (taskId: string) => void;
  selectedTaskId?: string;
  checkedTaskIds?: Set<string>;
  onToggleCheck?: (taskId: string, e: React.MouseEvent) => void;
  statuses?: import("@/types/status").Status[];
}

export function FolderTaskList({ 
  onSelectTask, 
  selectedTaskId = "1",
  checkedTaskIds = new Set(),
  onToggleCheck,
  statuses = []
}: FolderTaskListProps) {
  const { folderId } = useParams({ strict: false }) as { folderId: string; workspaceId: string };
  const { data, isLoading } = useGetFolderTasks(folderId);
  const [createOpen, setCreateOpen] = React.useState(false);

  // Sync TanStack Query into the flat hierarchy store
  const queryClient = useQueryClient();
  const setTasks = useHierarchyStore((s) => s.setTasks);
  React.useEffect(() => {
    if (data) {
      const fetchedTasks = data.pages.flatMap((page) => page.items);
      setTasks(folderId, fetchedTasks);
    }
  }, [data, folderId, setTasks]);

  // Read our UI truth directly from the flat store
  const storeTaskIds = useHierarchyStore((s) => s.tasksByParent[folderId] ?? EMPTY_ARRAY);
  const storeTasksMap = useHierarchyStore((s) => s.tasks);
  
  const tasks = React.useMemo(() => {
    return storeTaskIds.map(id => storeTasksMap[id]).filter(Boolean) as TaskRecord[];
  }, [storeTaskIds, storeTasksMap]);

  const sortableItems = React.useMemo(() => tasks.map(t => t.id), [tasks]);

  const batchUpdate = useBatchUpdateFolderTasks(folderId);

  // Fix: Memoize sensor options to prevent infinite updates in DndContext
  const pointerSensor = useSensor(PointerSensor, { activationConstraint: { distance: 8 } });
  const keyboardSensor = useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates });
  const sensors = useSensors(pointerSensor, keyboardSensor);

  function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    
    if (over && active.id !== over.id) {
      const oldIndex = tasks.findIndex(t => t.id === active.id);
      const newIndex = tasks.findIndex(t => t.id === over.id);
      
      const newOrder = arrayMove(tasks, oldIndex, newIndex);
      
      // Update UI Optimistically in Zustand
      setTasks(folderId, newOrder);

      // Update React Query cache across all filters so it doesn't snap back
      queryClient.setQueriesData({ queryKey: ["folderTasks", folderId] }, (old: any) => {
        if (!old || !old.pages) return old;
        return {
           ...old,
           pages: old.pages.map((page: any, i: number) => 
             i === 0 ? { ...page, items: newOrder } : page
           )
        };
      });

      // Perform Backend Update
      batchUpdate.mutate(
        newOrder.map((t, idx) => ({
          id: t.id,
          orderKey: String(idx).padStart(6, '0'), // Simple lexicographical ordering key
        }))
      );
    }
  }

  return (
    <div className="flex flex-col h-full w-full bg-background relative">

      {/* Filter / Search Bar Placeholder */}
      <div className="px-1.5 py-1 border-b border-border/50 shrink-0 flex items-center gap-1">
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

      {/* The Sleek Task List */}
      <div className="flex-1 overflow-y-auto p-1.5 space-y-0.5 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/10 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/30 [&::-webkit-scrollbar-track]:bg-transparent">
        {isLoading && <div className="p-4 text-xs text-muted-foreground text-center">Loading tasks...</div>}
        
        {!isLoading && tasks.length === 0 && (
          <div className="p-4 text-xs text-muted-foreground text-center">No tasks in this folder.</div>
        )}
        
        <DndContext 
          sensors={sensors}
          collisionDetection={closestCenter}
          onDragEnd={handleDragEnd}
        >
          <SortableContext 
            items={sortableItems}
            strategy={verticalListSortingStrategy}
          >
            {tasks.map((task) => (
              <SortableTaskItem 
                key={task.id}
                task={task}
                isSelected={selectedTaskId === task.id}
                isChecked={checkedTaskIds.has(task.id)}
                onSelect={() => onSelectTask?.(task.id)}
                onToggleCheck={onToggleCheck}
                statuses={statuses}
              />
            ))}
          </SortableContext>
        </DndContext>
        
        {/* Create Task Button at the end of the list */}
        <Dialog open={createOpen} onOpenChange={setCreateOpen}>
          <DialogTrigger asChild>
            <button className="w-full flex items-center justify-center py-2.5 rounded-md hover:bg-muted/50 text-muted-foreground hover:text-foreground transition-colors border border-transparent border-dashed hover:border-border mt-1">
              <span className="text-[12px] font-medium">Create new task</span>
            </button>
          </DialogTrigger>
          <DialogContent className="max-w-xl max-h-[85vh] overflow-y-auto">
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
