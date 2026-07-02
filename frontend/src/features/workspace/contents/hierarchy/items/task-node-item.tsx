import { observer } from "mobx-react-lite";
import { useLocation, useNavigate, useRouter } from "@tanstack/react-router";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { cn } from "@/lib/utils";
import { CheckSquare, MoreVertical } from "lucide-react";
import { useStore } from "@/stores/root.store";
import { SortableItem } from "../dnd/sortable-item";
import { EntityLayerType, EntityLayerType as EntityLayerConst,} from "@/types/entity-layer-type";
import { DynamicIcon } from "@/components/dynamic-icon";
import { TaskContextMenu } from "../hierarchy-components/context-menus/task-context-menu";
import { EntityMenuTrigger } from "../hierarchy-components/context-menus/shared";

interface TaskNodeItemProps {
  taskId: string;
  parentId: string;
  parentType: EntityLayerType;
  spaceId: string;
}

export const TaskNodeItem = observer(function TaskNodeItem({
  taskId,
  parentId,
  parentType,
  spaceId,
}: TaskNodeItemProps) {
  const rootStore = useStore();
  const task = rootStore.taskStore.getById(taskId);

  const navigate = useNavigate();
  const router = useRouter();
  const { workspaceId } = useWorkspace();
  const location = useLocation();

  if (!task) return null;

  const isActive = location.pathname.includes(`/tasks/${task.id}`);

  return (
    <SortableItem
      id={`task-${task.id}`}
      data={{
        ...task,
        type: EntityLayerConst.ProjectTask,
        id: task.id,
        parentId,
        parentType,
        spaceId,
      }}
    >
      <TaskContextMenu
        taskId={task.id}
        taskName={task.name}
        parentId={parentId}
        spaceId={spaceId}
      >
        <div
          className={cn(
            "flex items-center px-1 py-0.5 rounded-md transition-colors mb-px group border",
            isActive
              ? "bg-primary/5 text-primary border-primary/25"
              : "text-muted-foreground border-transparent hover:bg-muted/50 hover:text-foreground hover:border-border/30",
          )}
        >
          <button
            type="button"
            className="flex-1 text-left flex items-center outline-none select-none whitespace-nowrap"
            onMouseDown={() => {
              router.preloadRoute({
                to: "/workspaces/$workspaceId/tasks/$taskId",
                params: { workspaceId, taskId: task.id }
              });
            }}
            onClick={() => navigate({
                to: "/workspaces/$workspaceId/tasks/$taskId",
                params: { workspaceId, taskId: task.id }
              })}
          >
            <div className="w-1 h-1 rounded-full bg-muted-foreground/30 mr-1 shrink-0" />
            <div className="w-5 h-5 flex items-center justify-center shrink-0 mr-2">
              {task.icon ? (
                <DynamicIcon name={task.icon} color={task.color} size={14} />
              ) : (
                <CheckSquare className="h-3.5 w-3.5 opacity-60" />
              )}
            </div>
            <span className="text-[11px] font-semibold leading-tight">
              {task.name}
            </span>
          </button>
          <div className="flex items-center ml-1 shrink-0">
            <EntityMenuTrigger>
              <button
                type="button"
                className="h-4 w-4 p-0.5 flex items-center justify-center rounded-sm hover:bg-muted-foreground/10 text-muted-foreground hover:text-primary transition-colors"
                onClick={(e) => e.stopPropagation()}
              >
                <MoreVertical className="h-3.5 w-3.5" />
              </button>
            </EntityMenuTrigger>
          </div>
        </div>
      </TaskContextMenu>
    </SortableItem>
  );
});
