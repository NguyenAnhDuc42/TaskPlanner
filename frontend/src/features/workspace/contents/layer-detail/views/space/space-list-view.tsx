import {
  DragDropContext,
  Droppable,
  Draggable,
  type DropResult,
} from "@hello-pangea/dnd";
import { useMemo, useState, useEffect, useRef } from "react";
import { useNavigate, useParams } from "@tanstack/react-router";
import { cn } from "@/lib/utils";
import { format } from "date-fns";
import { Layers, Clock, User, Folder as FolderIcon } from "lucide-react";
import type { TaskViewData } from "../../layer-detail-types";
import { useMoveTaskToStatus } from "../task/task-api";
import { useMoveFolderToStatus } from "../folder/folder-api";
import { PriorityBadge } from "../../../../../../components/priority-badge";
import { StatusGroup } from "../../components/items/status-group";
import { buildColumns, calculateOrderKeys } from "./space-dnd-helpers";
import { Priority } from "@/types/priority";

interface SpaceListViewProps {
  viewData: TaskViewData;
}

export function SpaceListView({ viewData }: SpaceListViewProps) {
  const navigate = useNavigate();
  const { workspaceId } = useParams({ strict: false }) as any;

  const [localTasks, setLocalTasks] = useState(viewData.tasks || []);
  const [localFolders, setLocalFolders] = useState(viewData.folders || []);
  const isDraggingRef = useRef(false);
  const isDebouncingRef = useRef(false);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const { mutate: moveTaskToStatus, isPending: isMovingTask } =
    useMoveTaskToStatus();
  const { mutate: moveFolderToStatus, isPending: isMovingFolder } =
    useMoveFolderToStatus();

  useEffect(() => {
    if (isDraggingRef.current || isMovingTask || isMovingFolder || isDebouncingRef.current) return;
    setLocalTasks(viewData.tasks || []);
    setLocalFolders(viewData.folders || []);
  }, [viewData.tasks, viewData.folders, isMovingTask, isMovingFolder]);

  // Use the same buildColumns helper as the board!
  const columns = useMemo(() => {
    return buildColumns({
      ...viewData,
      tasks: localTasks,
      folders: localFolders,
    });
  }, [viewData, localTasks, localFolders]);

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
    )
      return;

    const srcColId = source.droppableId;
    const dstColId = destination.droppableId;

    const srcItems = [...(columns[srcColId] ?? [])];
    const dstItems =
      srcColId === dstColId ? srcItems : [...(columns[dstColId] ?? [])];

    const [movedItem] = srcItems.splice(source.index, 1);
    const isTask = "priority" in movedItem;

    const targetStatusId = dstColId === "unclassified" ? undefined : dstColId;
    const updatedItem = { ...movedItem, statusId: targetStatusId };

    dstItems.splice(destination.index, 0, updatedItem);

    const { previousItemOrderKey, nextItemOrderKey } = calculateOrderKeys(
      destination.index,
      draggableId,
      dstItems,
    );

    // Update local state immediately
    if (isTask) {
      setLocalTasks((prev) => {
        const filtered = prev.filter((t) => t.id !== draggableId);
        return [...filtered, updatedItem as any];
      });
    } else {
      setLocalFolders((prev) => {
        const filtered = prev.filter((f) => f.id !== draggableId);
        return [...filtered, updatedItem as any];
      });
    }

    // Persist (Debounced)
    isDebouncingRef.current = true;
    if (timeoutRef.current) clearTimeout(timeoutRef.current);
    
    timeoutRef.current = setTimeout(() => {
      isDebouncingRef.current = false;
      if (isTask) {
        moveTaskToStatus({
          taskId: draggableId,
          targetStatusId,
          previousItemOrderKey,
          nextItemOrderKey,
        });
      } else {
        moveFolderToStatus({
          folderId: draggableId,
          targetStatusId,
          previousItemOrderKey,
          nextItemOrderKey,
        });
      }
    }, 1000);
  }

  const handleFolderClick = (folderId: string) => {
    navigate({
      to: "/workspaces/$workspaceId/folders/$folderId",
      params: { workspaceId, folderId },
    });
  };

  const handleTaskClick = (taskId: string) => {
    navigate({
      to: "/workspaces/$workspaceId/tasks/$taskId",
      params: { workspaceId, taskId },
    });
  };

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
                      {items.map((item: any, idx: number) => {
                        const isTask = "priority" in item;
                        return (
                          <Draggable
                            key={item.id}
                            draggableId={item.id}
                            index={idx}
                          >
                            {(provided, snapshot) => (
                              <div
                                ref={provided.innerRef}
                                {...provided.draggableProps}
                                {...provided.dragHandleProps}
                                className={cn(
                                  snapshot.isDragging &&
                                    "[&_*]:transition-none opacity-80",
                                )}
                              >
                                {isTask ? (
                                  <TaskListRow
                                    task={item}
                                    isFirst={idx === 0}
                                    onClick={() => handleTaskClick(item.id)}
                                  />
                                ) : (
                                  <FolderListRow
                                    folder={item}
                                    isFirst={idx === 0}
                                    onClick={() => handleFolderClick(item.id)}
                                  />
                                )}
                              </div>
                            )}
                          </Draggable>
                        );
                      })}
                      {provided.placeholder}

                      {items.length === 0 && (
                        <div className="h-12 flex items-center justify-center text-xs text-[#4a4b53]">
                          No items in this section
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

function TaskListRow({
  task,
  onClick,
  isFirst,
}: {
  task: any;
  onClick: () => void;
  isFirst?: boolean;
}) {
  return (
    <div
      onClick={onClick}
      className={cn(
        "h-10 px-4 flex items-center gap-4 hover:bg-[#1c1c1f] transition-colors cursor-pointer border-t border-[#202127]",
        isFirst && "border-t-0",
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
            <span className="text-xs font-medium">
              {format(new Date(task.dueDate), "MMM d")}
            </span>
          </div>
        )}

        <div className="h-5 w-5 rounded-full bg-[#202127] flex items-center justify-center border border-[#2c2d35]">
          <User className="h-3 w-3 text-[#8a8b94]" />
        </div>
      </div>
    </div>
  );
}

function FolderListRow({
  folder,
  onClick,
  isFirst,
}: {
  folder: any;
  onClick: () => void;
  isFirst?: boolean;
}) {
  const folderColor = folder.color || "#3b82f6";

  return (
    <div
      onClick={onClick}
      className={cn(
        "h-10 px-4 flex items-center gap-4 hover:bg-[#1c1c1f] transition-colors cursor-pointer border-t border-[#202127] bg-[#141416]",
        isFirst && "border-t-0",
      )}
    >
      <div className="flex items-center gap-3 shrink-0">
        <Layers className="h-3.5 w-3.5 text-[#3b82f6]" />
        <span className="text-xs font-medium text-[#4a4b53] tracking-wider">
          FOLDER
        </span>
      </div>

      <div className="flex-1 min-w-0 flex items-center gap-2">
        <div
          className="shrink-0 h-5 w-5 rounded flex items-center justify-center border border-[#2c2d35]"
          style={{
            backgroundColor: `${folderColor}20`,
            color: folderColor,
          }}
        >
          {folder.icon ? (
            <span className="text-xs">{folder.icon}</span>
          ) : (
            <FolderIcon className="h-3 w-3" />
          )}
        </div>
        <span className="text-sm font-medium text-[#f5f5f7] truncate block">
          {folder.name}
        </span>
      </div>

      <div className="flex items-center gap-4 shrink-0">
        <div className="h-5 w-5 rounded-full bg-[#202127] flex items-center justify-center border border-[#2c2d35]">
          <User className="h-3 w-3 text-[#8a8b94]" />
        </div>
      </div>
    </div>
  );
}
