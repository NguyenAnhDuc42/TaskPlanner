import { observer } from "mobx-react-lite";
import { useStore } from "@/stores/root.store";
import { EntityLayerType } from "@/types/entity-layer-type";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";
import { TaskNodeItem } from "./task-node-item";

export const NodeTasksList = observer(function NodeTasksList({
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
  const rootStore = useStore();

  if (!isExpanded) return null;

  const isFolder = parentType === EntityLayerType.ProjectFolder;
  const tasks = rootStore.taskStore.all
    .filter((t) => !t.parentTaskId && (isFolder ? t.folderId === nodeId : (t.spaceId === nodeId && !t.folderId)))
    .sort((a, b) => ((a.orderKey ?? "") < (b.orderKey ?? "") ? -1 : 1));

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
