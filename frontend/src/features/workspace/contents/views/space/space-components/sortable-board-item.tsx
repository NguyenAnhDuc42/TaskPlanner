import React from "react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { Priority } from "@/types/priority";
import type { BoardItem } from "../space-api";
import { cn } from "@/lib/utils";
import { format, isBefore, startOfDay } from "date-fns";
import { InlinePriorityPicker } from "./inline-priority-picker";
import { DynamicIcon } from "@/components/dynamic-icon";
import { Calendar, User } from "lucide-react";

export interface BoardItemCardProps {
  item: BoardItem;
  onClick?: () => void;
  onPriorityChange?: (priority: Priority) => void;
  isDragging?: boolean;
  style?: React.CSSProperties;
  dragRef?: (node: HTMLElement | null) => void;
  dragProps?: any;
}

export const BoardItemCard = React.memo(function BoardItemCard({
  item,
  onClick,
  onPriorityChange,
  isDragging,
  style,
  dragRef,
  dragProps,
}: BoardItemCardProps) {
  const itemColor = item.color || (item.__type === "folder" ? "#3b82f6" : "#6b7280");
  const isOverdue = React.useMemo(() => {
    return item.dueDate ? isBefore(startOfDay(new Date(item.dueDate)), startOfDay(new Date())) : false;
  }, [item.dueDate]);

  const dateInfo = React.useMemo(() => {
    const show = !!item.startDate || !!item.dueDate;
    let text = "";
    if (show) {
      if (item.startDate && item.dueDate) {
        text = `${format(new Date(item.startDate), "MMM d")} - ${format(new Date(item.dueDate), "MMM d")}`;
      } else if (item.startDate) {
        text = `Start: ${format(new Date(item.startDate), "MMM d")}`;
      } else if (item.dueDate) {
        text = isOverdue 
          ? `Overdue: ${format(new Date(item.dueDate), "MMM d")}` 
          : `Due: ${format(new Date(item.dueDate), "MMM d")}`;
      }
    }
    const createdText = item.createdAt ? format(new Date(item.createdAt), "MMM d") : "";
    return { show, text, createdText };
  }, [item.startDate, item.dueDate, item.createdAt, isOverdue]);

  return (
    <div
      ref={dragRef}
      {...dragProps}
      onClick={onClick}
      className={cn(
        "group relative flex flex-col gap-1.5 p-2.5 rounded-lg cursor-grab active:cursor-grabbing select-none border outline-none shadow-sm shrink-0 w-full bg-muted/40 text-card-foreground border-border/50 hover:bg-muted/60",
        isDragging && "opacity-0 pointer-events-none border-transparent bg-transparent shadow-none"
      )}
      style={style}
    >
      <div className="flex flex-col gap-1.5 w-full h-full">
        {/* 1. Priority Picker & Assignee Avatar Row (Top) */}
        <div className="flex items-center justify-between text-[10px] font-medium leading-none">
          <InlinePriorityPicker
            priority={(item as any).priority as Priority}
            onPriorityChange={onPriorityChange || (() => {})}
          />
          <div className="flex items-center gap-2">
            <div className="h-4 w-4 rounded-full bg-white/[0.03] flex items-center justify-center border border-white/[0.05]">
              <User className="h-2.5 w-2.5 opacity-30 group-hover:opacity-50 transition-opacity" />
            </div>
          </div>
        </div>

        {/* 2. Custom Icon & Item Name */}
        <div className="flex items-center gap-2 mt-0.5">
          <div className="shrink-0 h-4.5 w-4.5 flex items-center justify-center" style={{ color: itemColor }}>
            <DynamicIcon
              name={item.icon || (item.__type === "folder" ? "Folder" : "Circle")}
              size={13}
              color={itemColor}
              className="stroke-[2.5]"
            />
          </div>
          <h4 className="text-[12px] font-medium leading-tight text-zinc-300 group-hover:text-white transition-colors truncate w-full">
            {item.name}
          </h4>
        </div>

        {/* 3. Badges Row: Type Tag & Calendar Date Pill (Below with nice spacing & creation date on right) */}
        <div className="flex items-center justify-between mt-1.5 w-full gap-2">
          <div className="flex items-center gap-1.5 flex-wrap">
            <span 
              className="px-1.5 py-0.5 rounded text-[8px] uppercase font-black tracking-widest border shrink-0"
              style={{
                backgroundColor: `${itemColor}0e`,
                color: itemColor,
                borderColor: `${itemColor}22`
              }}
            >
              {item.__type}
            </span>
            {dateInfo.show ? (
              <div 
                className={cn(
                  "flex items-center gap-1 text-[8px] font-black uppercase tracking-wider px-1.5 py-0.5 rounded border w-fit transition-colors leading-none",
                  isOverdue 
                    ? "bg-red-500/10 text-red-400 border-red-500/20" 
                    : "bg-white/[0.02] text-zinc-500 border-white/[0.04]"
                )}
              >
                <Calendar className={cn("h-2.5 w-2.5", isOverdue ? "opacity-90" : "opacity-40")} />
                <span>{dateInfo.text}</span>
              </div>
            ) : (
              <div 
                className="flex items-center gap-1 text-[8px] font-black uppercase tracking-wider px-1.5 py-0.5 rounded border w-fit transition-colors leading-none bg-white/[0.01] text-zinc-500/40 border-white/[0.03] select-none"
              >
                <Calendar className="h-2.5 w-2.5 opacity-20" />
                <span>No Date</span>
              </div>
            )}
          </div>
          
          {dateInfo.createdText && (
            <span className="text-[9px] text-muted-foreground/45 font-medium shrink-0 ml-auto select-none">
              {dateInfo.createdText}
            </span>
          )}
        </div>
      </div>
    </div>
  );
});

export const SortableBoardItem = React.memo(function SortableBoardItem({
  item,
  onTaskClick,
  onFolderClick,
  onPriorityChange,
}: {
  item: BoardItem;
  onTaskClick: (id: string) => void;
  onFolderClick: (id: string) => void;
  onPriorityChange: (id: string, type: "task" | "folder", priority: Priority) => void;
}) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({
    id: `${item.__type}-${item.id}`,
  });

  const style = {
    transform: isDragging ? undefined : CSS.Transform.toString(transform),
    transition: isDragging ? undefined : transition,
  };

  return (
    <BoardItemCard
      item={item}
      onClick={() => item.__type === "task" ? onTaskClick(item.id) : onFolderClick(item.id)}
      onPriorityChange={(p) => onPriorityChange(item.id, item.__type, p)}
      isDragging={isDragging}
      style={style}
      dragRef={setNodeRef}
      dragProps={{ ...attributes, ...listeners }}
    />
  );
});
