import React from "react";
import { useNavigate } from "@tanstack/react-router";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { cn } from "@/lib/utils";
import { CheckSquare, MoreHorizontal } from "lucide-react";
import { DropdownWrapper } from "@/components/dropdown-wrapper";
import { TaskMenu } from "../hierarchy-components/dropdown/task-menu";
import { SortableItem } from "../dnd/sortable-item";
import { clampName } from "../utils/name-utils";
import { EntityLayerType, EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";

import { useLocation } from "@tanstack/react-router";
import type { TaskHierarchy } from "../hierarchy-type";

interface TaskItemProps {
  task: TaskHierarchy;
  parentId: string;
  parentType: EntityLayerType;
  spaceId: string;
}

export const TaskItem = React.memo(function TaskItem({ task, parentId, parentType, spaceId }: TaskItemProps) {
  const navigate = useNavigate();
  const { workspaceId } = useWorkspace();
  const location = useLocation();
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
        <div className="w-5 h-5 flex items-center justify-center flex-shrink-0 mr-1.5">
          <CheckSquare className="h-3.5 w-3.5 opacity-60" />
        </div>
        <span className="truncate text-[11px] font-semibold flex-1 leading-tight">
          {clampName(task.name)}
        </span>
        <div className="opacity-0 group-hover:opacity-100 transition-opacity">
          <DropdownWrapper align="start" side="right" trigger={
              <button className="h-4 w-4 p-0.5 flex items-center justify-center rounded-sm hover:bg-muted-foreground/10 text-muted-foreground hover:text-primary transition-colors" onClick={(e) => e.stopPropagation()}>
                <MoreHorizontal className="h-3.5 w-3.5" />
              </button>
            }
          >
            <TaskMenu taskId={task.id} parentId={parentId} parentType={parentType} />
          </DropdownWrapper>
        </div>
      </div>
    </SortableItem>
  );
});
