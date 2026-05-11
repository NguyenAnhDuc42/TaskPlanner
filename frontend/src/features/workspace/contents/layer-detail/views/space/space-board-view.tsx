import {
  DragDropContext,
  Droppable,
  Draggable,
  type DropResult,
} from "@hello-pangea/dnd";
import { useState, useEffect, useRef } from "react";
import { useNavigate, useParams } from "@tanstack/react-router";
import { useQueryClient } from "@tanstack/react-query";
import { workspaceKeys } from "@/features/main/query-keys";
import { StatusGroup } from "../../components/items/status-group";
import { TaskItem } from "../../components/items/task-item";
import { FolderItem } from "../../components/items/folder-item";
import type { TaskViewData } from "../../layer-detail-types";
import { cn } from "@/lib/utils";
import { buildColumns, calculateOrderKeys } from "./space-dnd-helpers";
import { useMoveTaskToStatus } from "../task/task-api";
import { useMoveFolderToStatus } from "../folder/folder-api";
import { StatusCategory } from "@/types/status-category";

interface SpaceBoardViewProps {
  viewData: TaskViewData;
  spaceId: string;
}

export function SpaceBoardView({ viewData, spaceId }: SpaceBoardViewProps) {
  const navigate = useNavigate();
  const { workspaceId } = useParams({ strict: false }) as any;
  const queryClient = useQueryClient();

  const [columns, setColumns] = useState<Record<string, any[]>>(() =>
    buildColumns(viewData),
  );

  // Always-current snapshot — handleDragEnd reads this, not the stale closure
  const columnsRef = useRef(columns);
  columnsRef.current = columns;

  const isDraggingRef = useRef(false);
  const viewDataRef = useRef<TaskViewData | null>(null);

  const { mutate: moveTaskToStatus, isPending: isMovingTask } =
    useMoveTaskToStatus();
  const { mutate: moveFolderToStatus, isPending: isMovingFolder } =
    useMoveFolderToStatus();

  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const isDebouncingRef = useRef(false);
  const pendingMutationRef = useRef<(() => void) | null>(null);

  useEffect(() => {
    if (isDraggingRef.current || isMovingTask || isMovingFolder || isDebouncingRef.current) return;

    const prev = viewDataRef.current;
    const hasChanged =
      !prev ||
      prev.tasks !== viewData.tasks ||
      prev.folders !== viewData.folders ||
      prev.statuses !== viewData.statuses;

    if (!hasChanged) return;

    viewDataRef.current = viewData;
    setColumns(buildColumns(viewData));
  }, [viewData, isMovingTask, isMovingFolder]);

  useEffect(() => {
    return () => {
      if (timeoutRef.current) clearTimeout(timeoutRef.current);
      pendingMutationRef.current?.();
    };
  }, []);

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
    const targetStatusId = dstColId === "unclassified" ? undefined : dstColId;

    // 1. Snapshot pre-move arrays from ref — avoids stale closure on columns
    const srcItems = [...(columnsRef.current[srcColId] ?? [])];
    const dstItems =
      srcColId === dstColId
        ? srcItems
        : [...(columnsRef.current[dstColId] ?? [])];

    // 2. Calculate order keys from PRE-MOVE state
    //    calculateOrderKeys strips draggableId internally so same/cross-column
    //    both work correctly
    const { previousItemOrderKey, nextItemOrderKey } = calculateOrderKeys(
      destination.index,
      draggableId,
      dstItems,
    );

    // 3. Apply visual move
    const [movedItem] = srcItems.splice(source.index, 1);
    const updatedItem = { ...movedItem, statusId: targetStatusId };

    if (srcColId === dstColId) {
      srcItems.splice(destination.index, 0, updatedItem);
      setColumns((prev) => ({ ...prev, [srcColId]: srcItems }));
    } else {
      dstItems.splice(destination.index, 0, updatedItem);
      setColumns((prev) => ({
        ...prev,
        [srcColId]: srcItems,
        [dstColId]: dstItems,
      }));
    }

    // 4. Type check — task vs folder
    const isTask = "priority" in movedItem;

    // 5. Optimistic cache patch (only needed when status actually changes)
    if (srcColId !== dstColId) {
      queryClient.setQueryData(
        [...workspaceKeys.all, "space", spaceId, "items"],
        (old: any) => {
          if (!old) return old;
          const patch = (list: any[]) =>
            (list ?? []).map((item: any) =>
              item.id === draggableId
                ? { ...item, statusId: targetStatusId }
                : item,
            );
          return {
            ...old,
            tasks: patch(old.tasks),
            folders: patch(old.folders),
          };
        },
      );
    }

    // 6. Persist (Debounced)
    isDebouncingRef.current = true;
    if (timeoutRef.current) clearTimeout(timeoutRef.current);
    
    const persist = () => {
      isDebouncingRef.current = false;
      pendingMutationRef.current = null;
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
    };
    pendingMutationRef.current = persist;
    timeoutRef.current = setTimeout(persist, 1000);
  }

  function handleFolderClick(folderId: string) {
    navigate({
      to: "/workspaces/$workspaceId/folders/$folderId",
      params: { workspaceId, folderId },
    });
  }

  function handleTaskClick(taskId: string) {
    navigate({
      to: "/workspaces/$workspaceId/tasks/$taskId",
      params: { workspaceId, taskId },
    });
  }

  const statuses = viewData.statuses ?? [];
  const displayStatuses = [...statuses];
  if ((columns["unclassified"]?.length ?? 0) > 0) {
    displayStatuses.push({
      statusId: "unclassified",
      name: "Unclassified",
      color: "#6b7280",
      category: StatusCategory.NotStarted,
    });
  }

  return (
    <DragDropContext onDragStart={handleDragStart} onDragEnd={handleDragEnd}>
      <div className="h-full flex gap-4 p-6 overflow-x-auto overflow-y-auto no-scrollbar animate-in fade-in slide-in-from-bottom-2 duration-500">
        {displayStatuses.map((status) => {
          const items = columns[status.statusId] ?? [];
          return (
            <StatusGroup
              key={status.statusId}
              id={status.statusId}
              statusName={status.name}
              color={status.color}
              totalCount={items.length}
              className="w-[300px]"
            >
              <Droppable droppableId={status.statusId}>
                {(provided) => (
                  <div
                    ref={provided.innerRef}
                    {...provided.droppableProps}
                    className="flex flex-col min-h-[40px] max-h-[calc(100vh-250px)] overflow-y-auto no-scrollbar p-1"
                  >
                    {items.map((item: any, index: number) => {
                      const isTask = "priority" in item;
                      return (
                        <Draggable
                          key={item.id}
                          draggableId={item.id}
                          index={index}
                        >
                          {(provided, snapshot) => (
                            <div
                              ref={provided.innerRef}
                              {...provided.draggableProps}
                              {...provided.dragHandleProps}
                              style={{
                                ...provided.draggableProps.style,
                                opacity: snapshot.isDragging ? 0.8 : 1,
                              }}
                              className={cn(
                                snapshot.isDragging && "[&_*]:transition-none",
                              )}
                            >
                              {isTask ? (
                                <TaskItem
                                  task={item}
                                  onClick={() => handleTaskClick(item.id)}
                                />
                              ) : (
                                <FolderItem
                                  folder={item}
                                  onClick={() => handleFolderClick(item.id)}
                                />
                              )}
                            </div>
                          )}
                        </Draggable>
                      );
                    })}
                    {provided.placeholder}
                  </div>
                )}
              </Droppable>
            </StatusGroup>
          );
        })}
      </div>
    </DragDropContext>
  );
}
