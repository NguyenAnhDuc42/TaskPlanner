import { Plus, EyeOff, GripVertical } from "lucide-react";
import type { ReactNode } from "react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { cn } from "@/lib/utils";
import { StatusBadge } from "@/components/status-badge";
import type { Status } from "@/types/status";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

interface StatusGroupProps {
  id: string;
  statusName: string;
  color: string;
  totalCount: number;
  children: ReactNode;
  className?: string;
  onCreateTask?: () => void;
  onCreateFolder?: () => void;
  onHide?: () => void;
  draggable?: boolean;
}

export function StatusGroup({
  id,
  statusName,
  color,
  totalCount,
  children,
  className,
  onCreateTask,
  onCreateFolder,
  onHide,
  draggable = false,
}: Readonly<StatusGroupProps>) {
  const hasCreate = onCreateTask || onCreateFolder;
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id,
    data: { type: "column" },
    disabled: !draggable,
  });

  const style = { transform: CSS.Transform.toString(transform), transition };

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={cn(
        "shrink-0 flex flex-col bg-black/5 dark:bg-muted/20 rounded-lg border border-border/50 shadow-sm overflow-hidden transition-colors duration-300",
        isDragging && "opacity-40",
        className,
      )}
    >
      {/* Column Header */}
      <div className="flex items-center justify-between px-3 py-2 group/header border-b border-border bg-muted/10">
        <div className="flex items-center gap-2">
          {draggable && (
            <button
              type="button"
              className="text-muted-foreground/20 hover:text-muted-foreground/60 cursor-grab active:cursor-grabbing shrink-0 touch-none transition-colors"
              {...attributes}
              {...listeners}
            >
              <GripVertical className="h-3.5 w-3.5" />
            </button>
          )}
          <StatusBadge status={{ name: statusName, color: color } as Status} variant="outline" />
          <span className="text-[9px] font-black text-muted-foreground/40 px-2 py-0.5 rounded-md bg-white/2 border border-white/3">
            {totalCount}
          </span>
        </div>

        <div className="flex items-center gap-0.5 opacity-0 group-hover/header:opacity-100 transition-all duration-200">
          {onHide && (
            <button
              onClick={onHide}
              className="p-1.5 hover:bg-white/5 rounded-md text-muted-foreground/30 hover:text-foreground transition-all active:scale-90"
              title="Hide column"
            >
              <EyeOff className="h-3.5 w-3.5" />
            </button>
          )}
          {hasCreate && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <button className="p-1.5 hover:bg-white/5 rounded-md text-muted-foreground/30 hover:text-foreground transition-all active:scale-90">
                  <Plus className="h-3.5 w-3.5" />
                </button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-40 bg-popover border border-border shadow-md rounded-md p-1">
                {onCreateTask && (
                  <DropdownMenuItem
                    className="cursor-pointer flex items-center gap-2 text-xs p-1.5 rounded"
                    onClick={onCreateTask}
                  >
                    <Plus className="h-3.5 w-3.5 text-muted-foreground" />
                    <span className="font-medium">Create Task</span>
                  </DropdownMenuItem>
                )}
                {onCreateFolder && (
                  <DropdownMenuItem
                    className="cursor-pointer flex items-center gap-2 text-xs p-1.5 rounded"
                    onClick={onCreateFolder}
                  >
                    <svg className="h-3.5 w-3.5 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                      <path strokeLinecap="round" strokeLinejoin="round" d="M9 13h6m-3-3v6m-9 1V7a2 2 0 012-2h6l2 2h6a2 2 0 012 2v8a2 2 0 01-2 2H5a2 2 0 01-2-2z" />
                    </svg>
                    <span className="font-medium">Create Folder</span>
                  </DropdownMenuItem>
                )}
              </DropdownMenuContent>
            </DropdownMenu>
          )}
        </div>
      </div>

      {/* Items Area */}
      <div className="flex-1 flex flex-col min-h-0 pt-2">
        {children}
      </div>
    </div>
  );
}
