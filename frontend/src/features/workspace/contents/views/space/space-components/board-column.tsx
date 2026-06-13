import React, { useMemo, useState } from "react";
import { useDroppable } from "@dnd-kit/core";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";
import { Plus } from "lucide-react";
import { cn } from "@/lib/utils";
import { Priority } from "@/types/priority";
import { StatusGroup } from "./status-group";
import { SortableBoardItem } from "./sortable-board-item";
import type { BoardItem } from "../space-api";
import { Dialog, DialogContent } from "@/components/ui/dialog";
import { CreateTaskForm } from "@/features/workspace/components/forms/create-task-form";
import { CreateFolderForm } from "@/features/workspace/components/forms/create-folder-form";
import { EntityLayerType } from "@/types/entity-layer-type";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

export const BoardColumn = React.memo(function BoardColumn({
  statusId,
  name,
  color,
  category,
  items,
  spaceId,
  onTaskClick,
  onFolderClick,
  onPriorityChange,
  onDateChange,
}: {
  statusId: string;
  name: string;
  color: string;
  category: string;
  items: BoardItem[];
  spaceId: string;
  onTaskClick: (id: string) => void;
  onFolderClick: (id: string) => void;
  onPriorityChange: (id: string, type: "task" | "folder", priority: Priority) => void;
  onDateChange: (id: string, type: "task" | "folder", patches: { startDate?: string; dueDate?: string; clearStartDate?: boolean; clearDueDate?: boolean }) => void;
}) {
  const { setNodeRef, isOver } = useDroppable({
    id: statusId,
  });

  const itemIds = useMemo(() => 
    items
      .filter((i) => i && i.id)
      .map((i) => `${i.__type}-${i.id}`), 
    [items]
  );

  const [createOpen, setCreateOpen] = useState(false);
  const [createType, setCreateType] = useState<"task" | "folder" | null>(null);

  const handleOpenChange = (open: boolean) => {
    setCreateOpen(open);
    if (!open) {
      setCreateType(null);
    }
  };

  return (
    <StatusGroup
      id={statusId}
      statusName={name}
      color={color}
      category={category}
      totalCount={items.length}
      className="w-[280px] min-h-[400px] shrink-0 flex flex-col"
    >
      <div
        ref={setNodeRef}
        className={cn(
          "flex flex-col flex-1 overflow-y-auto px-2 pb-2 pt-1 gap-2 rounded-md transition-colors status-column-scrollable",
          "[&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-white/[0.05] [&::-webkit-scrollbar-thumb]:rounded-full hover:[&::-webkit-scrollbar-thumb]:bg-white/[0.15] [&::-webkit-scrollbar-track]:bg-transparent",
          isOver
            ? "bg-white/[0.02] border border-dashed border-border/60"
            : "border border-transparent",
        )}
      >
        <SortableContext items={itemIds} strategy={verticalListSortingStrategy}>
          {items
            .filter((item) => item && item.id)
            .map((item) => (
              <SortableBoardItem
                key={`${item.__type}-${item.id}`}
                item={item}
                onTaskClick={onTaskClick}
                onFolderClick={onFolderClick}
                onPriorityChange={onPriorityChange}
                onDateChange={onDateChange}
              />
            ))}
        </SortableContext>
      </div>

      {/* Render the button and dropdown OUTSIDE the droppable DND-active container area! */}
      <div className="px-2 pb-2 shrink-0">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button className="w-full flex items-center justify-center py-2 rounded-md hover:bg-white/[0.04] text-muted-foreground/60 hover:text-foreground transition-all border border-dashed border-border/50 hover:border-border shrink-0 mt-1 gap-1 cursor-pointer active:scale-[0.98]">
              <Plus className="h-3.5 w-3.5" />
              <span className="text-[11px] font-semibold">Create Item</span>
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-40 bg-popover border border-border shadow-md rounded-md p-1">
            <DropdownMenuItem
              className="cursor-pointer flex items-center gap-2 text-xs hover:bg-muted p-1.5 rounded transition-colors"
              onClick={() => {
                setCreateType("task");
                setCreateOpen(true);
              }}
            >
              <Plus className="h-3.5 w-3.5 text-muted-foreground" />
              <span className="font-medium text-foreground">Create Task</span>
            </DropdownMenuItem>
            <DropdownMenuItem
              className="cursor-pointer flex items-center gap-2 text-xs hover:bg-muted p-1.5 rounded transition-colors"
              onClick={() => {
                setCreateType("folder");
                setCreateOpen(true);
              }}
            >
              <svg className="h-3.5 w-3.5 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M9 13h6m-3-3v6m-9 1V7a2 2 0 012-2h6l2 2h6a2 2 0 012 2v8a2 2 0 01-2 2H5a2 2 0 01-2-2z" />
              </svg>
              <span className="font-medium text-foreground">Create Folder</span>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {/* Programmatic Dialog Wrapper */}
      <Dialog open={createOpen} onOpenChange={handleOpenChange}>
        <DialogContent className="max-w-2xl p-0" showCloseButton={false}>
          {createType === "task" ? (
            <CreateTaskForm
              parentId={spaceId}
              parentType={EntityLayerType.ProjectSpace}
              defaultStatusId={statusId === "unclassified" ? undefined : statusId}
              onSuccess={() => {
                setCreateOpen(false);
                setCreateType(null);
              }}
              onCancel={() => {
                setCreateOpen(false);
                setCreateType(null);
              }}
            />
          ) : createType === "folder" ? (
            <CreateFolderForm
              spaceId={spaceId}
              onSuccess={() => {
                setCreateOpen(false);
                setCreateType(null);
              }}
              onCancel={() => {
                setCreateOpen(false);
                setCreateType(null);
              }}
            />
          ) : null}
        </DialogContent>
      </Dialog>
    </StatusGroup>
  );
});