import React, { useMemo, useState } from "react";
import { useDroppable } from "@dnd-kit/core";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";
import { Plus } from "lucide-react";
import { cn } from "@/lib/utils";
import { Priority } from "@/types/priority";
import { StatusGroup } from "./status-group";
import { SortableBoardItem } from "./sortable-board-item";
import type { BoardItem } from "../space-board-types";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { CreateTaskForm } from "@/features/workspace/components/forms/create-task-form";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";

export const BoardColumn = React.memo(function BoardColumn({
  statusId,
  name,
  color,
  items,
  spaceId,
  selectedItemId,
  onTaskClick,
  onPriorityChange,
  onDateChange,
  onHide,
  draggable,
  fullWidth,
}: {
  statusId: string;
  name: string;
  color: string;
  items: BoardItem[];
  spaceId: string;
  selectedItemId?: string;
  onTaskClick: (id: string) => void;
  onPriorityChange: (id: string, priority: Priority) => void;
  onDateChange: (id: string, patches: { startDate?: string | null; dueDate?: string | null }) => void;
  onHide?: () => void;
  draggable?: boolean;
  fullWidth?: boolean;
}) {
  const { setNodeRef, isOver } = useDroppable({ id: `zone:${statusId}`, data: { type: "card-zone" } });
  const itemIds = useMemo(() =>
    items.filter((i) => i?.id).map((i) => `task-${i.id}`),
    [items]
  );

  const { canCreateContent } = useWorkspaceRole();
  const [createOpen, setCreateOpen] = useState(false);

  return (
    <StatusGroup
      id={statusId}
      statusName={name}
      color={color}
      totalCount={items.length}
      className={cn("min-h-[400px] shrink-0 flex flex-col", fullWidth ? "w-full snap-center" : "w-[280px]")}
      onCreateTask={canCreateContent ? () => setCreateOpen(true) : undefined}
      onHide={onHide}
      draggable={draggable}
    >
      <div
        ref={setNodeRef}
        className={cn(
          "flex flex-col flex-1 overflow-y-auto px-2 pb-2 pt-1 gap-2 rounded-md transition-colors status-column-scrollable",
          "[&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-white/5 [&::-webkit-scrollbar-thumb]:rounded-full hover:[&::-webkit-scrollbar-thumb]:bg-white/15 [&::-webkit-scrollbar-track]:bg-transparent",
          isOver ? "bg-white/2 border border-dashed border-border/60" : "border border-transparent",
        )}
      >
        <SortableContext items={itemIds} strategy={verticalListSortingStrategy}>
          {items.filter((item) => item?.id).map((item) => (
            <SortableBoardItem
              key={`task-${item.id}`}
              item={item}
              isSelected={selectedItemId === item.id}
              onTaskClick={onTaskClick}
              onPriorityChange={onPriorityChange}
              onDateChange={onDateChange}
            />
          ))}
        </SortableContext>
      </div>

      {canCreateContent && (
        <div className="px-2 pb-2 shrink-0">
          <button
            onClick={() => setCreateOpen(true)}
            className="w-full flex items-center justify-center py-2 rounded-md hover:bg-white/4 text-muted-foreground/60 hover:text-foreground transition-all border border-dashed border-border/50 hover:border-border shrink-0 mt-1 gap-1 cursor-pointer active:scale-[0.98]"
          >
            <Plus className="h-3.5 w-3.5" />
            <span className="text-[11px] font-semibold">Create Task</span>
          </button>
        </div>
      )}

      <DialogFormWrapper
        open={createOpen}
        onOpenChange={setCreateOpen}
        title="Create New Task"
        trigger={null}
      >
        <CreateTaskForm
          parentId={spaceId}
          parentType={EntityLayerType.ProjectSpace}
          defaultStatusId={statusId === "unclassified" ? undefined : statusId}
          onCancel={() => setCreateOpen(false)}
        />
      </DialogFormWrapper>
    </StatusGroup>
  );
});
