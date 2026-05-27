import { useMemo } from "react";
import { useDroppable } from "@dnd-kit/core";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";
import { cn } from "@/lib/utils";
import { Priority } from "@/types/priority";
import { StatusGroup } from "./status-group";
import { SortableBoardItem } from "./sortable-board-item";
import type { BoardItem } from "../space-api";

export function BoardColumn({
  statusId,
  name,
  color,
  items,
  onTaskClick,
  onFolderClick,
  onPriorityChange,
}: {
  statusId: string;
  name: string;
  color: string;
  items: BoardItem[];
  onTaskClick: (id: string) => void;
  onFolderClick: (id: string) => void;
  onPriorityChange: (id: string, type: "task" | "folder", priority: Priority) => void;
}) {
  const { setNodeRef, isOver } = useDroppable({
    id: statusId,
  });

  const itemIds = useMemo(() => items.map((i) => i.id), [items]);

  return (
    <StatusGroup
      id={statusId}
      statusName={name}
      color={color}
      totalCount={items.length}
      className="w-[280px] min-h-[400px] shrink-0"
    >
      <div
        ref={setNodeRef}
        className={cn(
          "flex flex-col flex-1 overflow-y-auto p-1 gap-2 rounded-md transition-colors status-column-scrollable",
          "[&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-white/[0.05] [&::-webkit-scrollbar-thumb]:rounded-full hover:[&::-webkit-scrollbar-thumb]:bg-white/[0.15] [&::-webkit-scrollbar-track]:bg-transparent",
          isOver
            ? "bg-white/[0.02] border border-dashed border-border/60"
            : "border border-transparent",
        )}
      >
        <SortableContext items={itemIds} strategy={verticalListSortingStrategy}>
          {items.map((item) => (
            <SortableBoardItem
              key={item.id}
              item={item}
              onTaskClick={onTaskClick}
              onFolderClick={onFolderClick}
              onPriorityChange={onPriorityChange}
            />
          ))}
        </SortableContext>
      </div>
    </StatusGroup>
  );
}
