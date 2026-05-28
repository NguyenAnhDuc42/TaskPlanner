import { useMemo, useState } from "react";
import { useDroppable } from "@dnd-kit/core";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";
import { Plus } from "lucide-react";
import { cn } from "@/lib/utils";
import { Priority } from "@/types/priority";
import { StatusGroup } from "./status-group";
import { SortableBoardItem } from "./sortable-board-item";
import type { BoardItem } from "../space-api";
import { Dialog, DialogContent, DialogTrigger } from "@/components/ui/dialog";
import { CreateTaskForm } from "@/features/workspace/components/forms/create-task-form";
import { EntityLayerType } from "@/types/entity-layer-type";

export function BoardColumn({
  statusId,
  name,
  color,
  items,
  spaceId,
  onTaskClick,
  onFolderClick,
  onPriorityChange,
}: {
  statusId: string;
  name: string;
  color: string;
  items: BoardItem[];
  spaceId: string;
  onTaskClick: (id: string) => void;
  onFolderClick: (id: string) => void;
  onPriorityChange: (id: string, type: "task" | "folder", priority: Priority) => void;
}) {
  const { setNodeRef, isOver } = useDroppable({
    id: statusId,
  });

  const itemIds = useMemo(() => items.map((i) => i.id), [items]);

  const [createOpen, setCreateOpen] = useState(false);

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

        {/* Create Item Button */}
        <Dialog open={createOpen} onOpenChange={setCreateOpen}>
          <DialogTrigger asChild>
            <button className="w-full flex items-center justify-center py-2 rounded-md hover:bg-white/[0.04] text-muted-foreground/60 hover:text-foreground transition-all border border-dashed border-border/50 hover:border-border shrink-0 mt-1 gap-1 cursor-pointer active:scale-[0.98]">
              <Plus className="h-3.5 w-3.5" />
              <span className="text-[11px] font-semibold">Create Item</span>
            </button>
          </DialogTrigger>
          <DialogContent className="max-w-xl max-h-[85vh] overflow-y-auto">
            <CreateTaskForm
              parentId={spaceId}
              parentType={EntityLayerType.ProjectSpace}
              defaultStatusId={statusId === "unclassified" ? undefined : statusId}
              onSuccess={() => setCreateOpen(false)}
              onCancel={() => setCreateOpen(false)}
            />
          </DialogContent>
        </Dialog>
      </div>
    </StatusGroup>
  );
}
