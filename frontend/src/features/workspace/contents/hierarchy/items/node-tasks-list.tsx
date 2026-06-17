import React from "react";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { useGetNodeTasksQuery, useTasksByParent } from "../hierarchy-api";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useDispatch } from "react-redux";
import { folderSlice, spaceSlice } from "@/store/entityStore";
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
  const dispatch = useDispatch();
  
  // 1. Fetch child tasks using Redux Query
  const { isLoading } = useGetNodeTasksQuery(
    { workspaceId: workspaceId || "", nodeId, parentType, cursor: null },
    { skip: !isExpanded } // Only query if expanded
  );
    
  // 2. Select tasks dynamically from our centralized store
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

  // Always render SortableContext even when empty — required so DND can drop into empty layers
  return (
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
  );
});
