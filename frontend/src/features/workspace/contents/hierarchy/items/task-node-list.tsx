import React from "react";
import { useDispatch } from "react-redux";
import type { AppDispatch } from "@/store";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useGetNodeTasksQuery, useTasksByParent, hierarchyApi } from "../hierarchy-api";
import { EntityLayerType } from "@/types/entity-layer-type";
import { folderSlice, spaceSlice } from "@/store/entityStore";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";
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
  const dispatch = useDispatch<AppDispatch>();

  const { isLoading, isFetching, data } = useGetNodeTasksQuery(
    { workspaceId: workspaceId || "", nodeId, parentType, cursor: null },
    { skip: !isExpanded },
  );

  const tasks = useTasksByParent(nodeId);

  React.useEffect(() => {
    if (isExpanded && !isLoading && tasks.length === 0) {
      if (parentType === EntityLayerType.ProjectFolder) {
        dispatch(folderSlice.actions.upsert({ id: nodeId, hasTasks: false }));
      } else if (parentType === EntityLayerType.ProjectSpace) {
        dispatch(spaceSlice.actions.upsert({ id: nodeId, hasTasks: false }));
      }
    }
  }, [isExpanded, isLoading, tasks.length, parentType, nodeId, dispatch]);

  if (!isExpanded) return null;

  if (isLoading && tasks.length === 0) {
    return (
      <div className="flex flex-col gap-0.5">
        {[1, 2, 3].map((i) => (
          <TaskSkeleton key={i} />
        ))}
      </div>
    );
  }

  const loadMore = () => {
    if (!data?.nextCursor || isFetching) return;
    dispatch(
      hierarchyApi.endpoints.getNodeTasks.initiate(
        { workspaceId: workspaceId || "", nodeId, parentType, cursor: data.nextCursor },
        { subscribe: false }
      ),
    );
  };

  return (
    <>
      <SortableContext
        items={tasks.map((t) => `task-${t.id}`)}
        strategy={verticalListSortingStrategy}
      >
        <div className="flex flex-col">
          {tasks.map((t) => (
            <TaskNodeItem
              key={t.id}
              taskId={t.id}
              parentId={nodeId}
              parentType={parentType}
              spaceId={spaceId}
            />
          ))}
        </div>
      </SortableContext>

      {data?.hasNextPage && (
        <button
          onClick={loadMore}
          disabled={isFetching}
          className="text-[10px] text-muted-foreground/40 hover:text-primary py-0.5 px-1 text-left transition-colors disabled:opacity-40 font-mono uppercase tracking-tight"
        >
          {isFetching ? "Loading..." : "Load more"}
        </button>
      )}
    </>
  );
});
