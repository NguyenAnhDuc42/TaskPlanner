import React from "react";
import { useNavigate } from "@tanstack/react-router";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { cn } from "@/lib/utils";
import { CheckSquare, MoreHorizontal } from "lucide-react";
import { useSelector } from "react-redux";
import { taskSelectors } from "@/store/entityStore";
import type { RootState } from "@/store";

import { SortableItem } from "../dnd/sortable-item";
import { FadeTruncate } from "@/components/fade-truncate";
import {
  EntityLayerType,
  EntityLayerType as EntityLayerConst,
} from "@/types/entity-layer-type";

import { useLocation } from "@tanstack/react-router";
import { DynamicIcon } from "@/components/dynamic-icon";
import { TaskContextMenu } from "../hierarchy-components/context-menus/task-context-menu";
import { EntityMenuTrigger } from "../hierarchy-components/context-menus/shared";

interface TaskNodeItemProps {
  taskId: string;
  parentId: string;
  parentType: EntityLayerType;
  spaceId: string;
}

export const TaskNodeItem = React.memo(function TaskNodeItem({
  taskId,
  parentId,
  parentType,
  spaceId,
}: TaskNodeItemProps) {
  // Select Task strictly from Redux
  const task = useSelector((state: RootState) => taskSelectors.selectById(state, taskId));
  
  const navigate = useNavigate();
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
      >
        <div
          className={cn(
            "flex items-center w-full px-1 py-0.5 rounded-sm transition-colors cursor-pointer mb-px group",
            isActive
              ? "text-primary bg-primary/10"
              : "text-muted-foreground hover:bg-muted hover:text-foreground",
          )}
          onClick={() =>
            navigate({ to: `/workspaces/${workspaceId}/tasks/${task.id}` })
          }
        >
          <div className="w-5 h-5 flex items-center justify-center shrink-0 mr-2">
            {task.icon ? (
              <DynamicIcon name={task.icon} color={task.color} size={14} />
            ) : (
              <CheckSquare className="h-3.5 w-3.5 opacity-60" />
            )}
          </div>
          <FadeTruncate
            text={task.name}
            className="text-[11px] font-semibold flex-1 leading-tight"
          />
          <div className="flex items-center gap-0.5 min-w-fit">
            <div className="w-0 group-hover:w-4 overflow-hidden opacity-0 group-hover:opacity-100 transition-all duration-300 ease-in-out">
              <EntityMenuTrigger>
                <button
                  className="h-4 w-4 p-0.5 flex items-center justify-center rounded-sm hover:bg-muted-foreground/10 text-muted-foreground hover:text-primary transition-colors"
                  onClick={(e) => e.stopPropagation()}
                >
                  <MoreHorizontal className="h-3.5 w-3.5" />
                </button>
              </EntityMenuTrigger>
            </div>
          </div>
        </div>
      </TaskContextMenu>
    </SortableItem>
  );
});
