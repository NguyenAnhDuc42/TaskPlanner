import React from "react";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { useNodeTasks } from "../hierarchy-api";
import { EntityLayerType } from "@/types/entity-layer-type";
import { Loader2, Plus } from "lucide-react";
import {
  SortableContext,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { TaskNodeItem } from "./task-node-item";

const TaskSkeleton = () => (
  <div className="flex items-center gap-2 pl-2 py-1 opacity-20 animate-pulse">
    <div className="h-3.5 w-4 bg-muted-foreground/30 rounded-sm" />
    <div className="h-2.5 w-24 bg-muted-foreground/30 rounded-full" />
  </div>
);

export const NodeTasksList = React.memo(function NodeTasksList({
  nodeId,
  parentType,
  isExpanded,
  spaceId,
}: {
  nodeId: string;
  parentType: EntityLayerType;
  isExpanded: boolean;
  spaceId: string;
}) {
  const { workspaceId } = useWorkspace();
  const { data, fetchNextPage, hasNextPage, isFetchingNextPage, isLoading } =
    useNodeTasks(workspaceId, nodeId, parentType);

  if (!isExpanded) return null;

  if (isLoading) {
    return (
      <div className="flex flex-col gap-0.5">
        {[1, 2, 3].map((i) => (
          <TaskSkeleton key={i} />
        ))}
      </div>
    );
  }

  const allTasks = data?.pages.flatMap((page) => page.tasks) || [];

  return (
    <SortableContext
      items={allTasks.map((t) => `task-${t.id}`)}
      strategy={verticalListSortingStrategy}
    >
      <div className="flex flex-col">
        {allTasks.map((task) => (
          <TaskNodeItem
            key={task.id}
            task={task}
            parentId={nodeId}
            parentType={parentType}
            spaceId={spaceId}
          />
        ))}
      </div>
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
    </SortableContext>
  );
});
