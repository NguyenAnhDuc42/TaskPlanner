import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { useNodeTasks } from "../hierarchy-api";
import { EntityLayerType } from "@/types/entity-layer-type";
import { Loader2, Plus } from "lucide-react";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";
import { TaskItem } from "./task-item";

export function NodeTasksList({nodeId, parentType, isExpanded,}: {nodeId: string; parentType: EntityLayerType; isExpanded: boolean;}) {
  const { workspaceId } = useWorkspace();
  const {
    data,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isLoading,
  } = useNodeTasks(workspaceId, nodeId, parentType);

  if (!isExpanded) return null;

  if (isLoading) {
    return (
      <div className="flex items-center gap-2 pl-6 py-1 opacity-50">
        <Loader2 className="h-3 w-3 animate-spin" />
        <span className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">
          Loading Tasks...
        </span>
      </div>
    );
  }

  const allTasks = data?.pages.flatMap((page) => page.tasks) || [];

  return (
    <SortableContext items={allTasks.map(t => `task-${t.id}`)} strategy={verticalListSortingStrategy}>
      {allTasks.map((task) => (
        <TaskItem key={task.id} task={task} parentId={nodeId} parentType={parentType} />
      ))}
      {hasNextPage && (
        <button
          onClick={(e) => {
            e.stopPropagation();
            fetchNextPage();
          }}
          disabled={isFetchingNextPage}
          className="flex items-center gap-2 pl-6 py-1 text-[10px] font-bold uppercase tracking-widest text-muted-foreground hover:text-primary transition-colors disabled:opacity-50"
        >
          {isFetchingNextPage ? (
            <Loader2 className="h-3 w-3 animate-spin" />
          ) : (
            <Plus className="h-3 w-3" />
          )}
          Load More
        </button>
      )}
      {allTasks.length === 0 && !isLoading && (
        <div className="pl-6 py-1 text-[10px] font-semibold text-muted-foreground/40 italic">
          No tasks
        </div>
      )}
    </SortableContext>
  );
}
