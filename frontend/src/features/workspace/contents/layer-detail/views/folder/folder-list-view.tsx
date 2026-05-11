import { DragDropContext, Droppable, Draggable, type DropResult } from "@hello-pangea/dnd";
import { useState, useEffect, useRef, useMemo } from "react";
import { useNavigate, useParams } from "@tanstack/react-router";
import { cn } from "@/lib/utils";
import { format } from "date-fns";
import { Clock, User } from "lucide-react";
import type { TaskViewData } from "../../layer-detail-types";
import { useMoveTaskToStatus } from "../task/task-api";
import { calculateOrderKeys, buildColumns } from "./folder-dnd-helpers";

import { StatusGroup } from "../../components/items/status-group";
import { Priority } from "@/types/priority";
import { PriorityBadge } from "@/components/priority-badge";

interface FolderListViewProps {
  viewData: TaskViewData;
}

export function FolderListView({ viewData }: FolderListViewProps) {
  const navigate = useNavigate();
  const { workspaceId } = useParams({ strict: false }) as any;

  // Use columns state to track order per status group
  const [columns, setColumns] = useState<Record<string, any[]>>(() => buildColumns(viewData));
  const isDraggingRef = useRef(false);
  const isDebouncingRef = useRef(false);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const { mutate: moveTaskToStatus, isPending: isMovingTask } = useMoveTaskToStatus();

  useEffect(() => {
    if (isDraggingRef.current || isMovingTask || isDebouncingRef.current) return;
    setColumns(buildColumns(viewData));
  }, [viewData, isMovingTask]);

  const statuses = viewData.statuses ?? [];
  const displayStatuses = [...statuses];
  if ((columns["unclassified"]?.length ?? 0) > 0) {
    displayStatuses.push({
      statusId: "unclassified",
      name: "Unclassified",
      color: "#6b7280",
    } as any);
  }

  function handleDragStart() {
    isDraggingRef.current = true;
  }

  function handleDragEnd(result: DropResult) {
    isDraggingRef.current = false;
    const { source, destination, draggableId } = result;
    if (!destination) return;

    if (
      source.droppableId === destination.droppableId &&
      source.index === destination.index
    ) return;

    const srcColId = source.droppableId;
    const dstColId = destination.droppableId;

    const srcItems = [...(columns[srcColId] ?? [])];
    const dstItems = srcColId === dstColId ? srcItems : [...(columns[dstColId] ?? [])];

    const [movedItem] = srcItems.splice(source.index, 1);
    const targetStatusId = dstColId === "unclassified" ? undefined : dstColId;
    const updatedItem = { ...movedItem, statusId: targetStatusId };

    dstItems.splice(destination.index, 0, updatedItem);

    // Update local state immediately preserving order!
    if (srcColId === dstColId) {
      setColumns((prev) => ({ ...prev, [srcColId]: dstItems }));
    } else {
      setColumns((prev) => ({
        ...prev,
        [srcColId]: srcItems,
        [dstColId]: dstItems,
      }));
    }

    const { previousItemOrderKey, nextItemOrderKey } = calculateOrderKeys(
      destination.index,
      draggableId,
      dstItems
    );

    // Persist (Debounced)
    isDebouncingRef.current = true;
    if (timeoutRef.current) clearTimeout(timeoutRef.current);
    
    timeoutRef.current = setTimeout(() => {
      isDebouncingRef.current = false;
      moveTaskToStatus({
        taskId: draggableId,
        targetStatusId,
        previousItemOrderKey,
        nextItemOrderKey,
      });
    }, 1000);
  }

  function handleTaskClick(taskId: string) {
    navigate({
      to: "/workspaces/$workspaceId/tasks/$taskId",
      params: { workspaceId, taskId },
    });
  }

  return (
    <DragDropContext onDragStart={handleDragStart} onDragEnd={handleDragEnd}>
      <div className="h-full flex flex-col bg-[#0a0a0b] overflow-y-auto no-scrollbar">
        <div className="p-6 space-y-8">
          {displayStatuses.map((status) => {
            const items = columns[status.statusId] ?? [];
            return (
              <StatusGroup
                key={status.statusId}
                id={status.statusId}
                statusName={status.name}
                color={status.color}
                totalCount={items.length}
                className="w-full"
              >
                <Droppable droppableId={status.statusId}>
                  {(provided) => (
                    <div 
                      ref={provided.innerRef} 
                      {...provided.droppableProps}
                      className="flex flex-col min-h-[40px]"
                    >
                      {items.map((task: any, idx: number) => (
                        <Draggable key={task.id} draggableId={task.id} index={idx}>
                          {(provided, snapshot) => (
                            <div
                              ref={provided.innerRef}
                              {...provided.draggableProps}
                              {...provided.dragHandleProps}
                              className={cn(
                                snapshot.isDragging && "[&_*]:transition-none opacity-80"
                              )}
                            >
                              <TaskListRow 
                                task={task} 
                                isFirst={idx === 0}
                                onClick={() => handleTaskClick(task.id)}
                              />
                            </div>
                          )}
                        </Draggable>
                      ))}
                      {provided.placeholder}
                      
                      {items.length === 0 && (
                        <div className="h-12 flex items-center justify-center text-xs text-[#4a4b53]">
                           No tasks in this section
                        </div>
                      )}
                    </div>
                  )}
                </Droppable>
              </StatusGroup>
            );
          })}
        </div>
      </div>
    </DragDropContext>
  );
}

function TaskListRow({ task, onClick, isFirst }: { task: any; onClick: () => void; isFirst?: boolean }) {
  return (
    <div 
      onClick={onClick}
      className={cn(
        "h-10 px-4 flex items-center gap-4 hover:bg-[#1c1c1f] transition-colors cursor-pointer border-t border-[#202127]",
        isFirst && "border-t-0"
      )}
    >
      {/* Priority & ID */}
      <div className="flex items-center gap-3 shrink-0">
         <PriorityBadge priority={task.priority as Priority} />
         <span className="text-xs font-medium text-[#4a4b53] tracking-wider min-w-[50px]">
            {`T-${task.id.slice(0, 4).toUpperCase()}`}
         </span>
      </div>

      {/* Title */}
      <div className="flex-1 min-w-0">
         <span className="text-sm font-medium text-[#f5f5f7] truncate block">
           {task.name}
         </span>
      </div>

      {/* Metadata */}
      <div className="flex items-center gap-4 shrink-0">
         {task.dueDate && (
           <div className="flex items-center gap-1.5 text-[#4a4b53]">
             <Clock className="h-3.5 w-3.5" />
             <span className="text-xs font-medium">{format(new Date(task.dueDate), "MMM d")}</span>
           </div>
         )}
         
         <div className="h-5 w-5 rounded-full bg-[#202127] flex items-center justify-center border border-[#2c2d35]">
           <User className="h-3 w-3 text-[#8a8b94]" />
         </div>
      </div>
    </div>
  );
}
