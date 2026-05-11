import { DndContext, type DragEndEvent, PointerSensor, useSensor, useSensors, DragOverlay } from "@dnd-kit/core";
import { SortableContext, verticalListSortingStrategy, useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { useMemo, useState, useEffect } from "react";
import { useNavigate, useParams } from "@tanstack/react-router";
import { cn } from "@/lib/utils";
import { format } from "date-fns";
import { Layers, Clock, User, Folder as FolderIcon, SignalLow, SignalMedium, SignalHigh, AlertTriangle, Circle } from "lucide-react";
import type { TaskViewData } from "../../layer-detail-types";
import { useUpdateTask } from "../task/task-api";
import { useUpdateFolder } from "../folder/folder-api";

interface SpaceListViewProps {
  viewData: TaskViewData;
}

export function SpaceListView({ viewData }: SpaceListViewProps) {
  const navigate = useNavigate();
  const { workspaceId } = useParams({ strict: false }) as any;

  const [localTasks, setLocalTasks] = useState(viewData.tasks || []);
  const [localFolders, setLocalFolders] = useState(viewData.folders || []);
  const [activeId, setActiveId] = useState<string | null>(null);

  useEffect(() => {
    setLocalTasks(viewData.tasks || []);
    setLocalFolders(viewData.folders || []);
  }, [viewData.tasks, viewData.folders]);

  const groups = useMemo(() => {
    const { statuses = [] } = viewData;
    const statusIds = statuses.map((s: any) => s.statusId);

    const unclassifiedTasks = localTasks.filter((t: any) => !statusIds.includes(t.statusId));
    const unclassifiedFolders = localFolders.filter((f: any) => !statusIds.includes(f.statusId));

    const mappedGroups = statuses.map((status: any) => ({
      statusId: status.statusId,
      statusName: status.name,
      color: status.color,
      tasks: localTasks.filter((t: any) => t.statusId === status.statusId),
      folders: localFolders.filter((f: any) => f.statusId === status.statusId),
    }));

    if (unclassifiedTasks.length > 0 || unclassifiedFolders.length > 0) {
      mappedGroups.push({
        statusId: "unclassified",
        statusName: "Unclassified",
        color: "#4a4b53",
        tasks: unclassifiedTasks,
        folders: unclassifiedFolders,
      });
    }

    return mappedGroups;
  }, [viewData.statuses, localTasks, localFolders]);

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 8,
      },
    })
  );

  const { mutate: updateTask } = useUpdateTask();
  const { mutate: updateFolder } = useUpdateFolder();

  function handleDragStart(event: any) {
    setActiveId(event.active.id);
  }

  function handleDragEnd(event: DragEndEvent) {
    setActiveId(null);
    const { active, over } = event;
    if (!over) return;

    const itemId = active.id as string;
    let newStatusId = over.id as string;
    
    const overTask = localTasks.find(t => t.id === newStatusId);
    const overFolder = localFolders.find(f => f.id === newStatusId);
    
    if (overTask) newStatusId = overTask.statusId || "unclassified";
    else if (overFolder) newStatusId = overFolder.statusId || "unclassified";

    const isTask = localTasks.some(t => t.id === itemId);
    const isFolder = localFolders.some(f => f.id === itemId);

    const finalStatusId = newStatusId === "unclassified" ? undefined : newStatusId;

    if (isTask) {
      setLocalTasks(prev => prev.map(t => t.id === itemId ? { ...t, statusId: finalStatusId } : t));
      updateTask({ taskId: itemId, statusId: finalStatusId });
    }
    if (isFolder) {
      setLocalFolders(prev => prev.map(f => f.id === itemId ? { ...f, statusId: finalStatusId } : f));
      updateFolder({ folderId: itemId, statusId: finalStatusId });
    }
  }

  const handleFolderClick = (folderId: string) => {
    navigate({ to: "/workspaces/$workspaceId/folders/$folderId", params: { workspaceId, folderId } });
  };

  const handleTaskClick = (taskId: string) => {
    navigate({ to: "/workspaces/$workspaceId/tasks/$taskId", params: { workspaceId, taskId } });
  };

  const activeTask = localTasks.find(t => t.id === activeId);
  const activeFolder = localFolders.find(f => f.id === activeId);

  return (
    <DndContext sensors={sensors} onDragStart={handleDragStart} onDragEnd={handleDragEnd}>
      <div className="h-full overflow-auto bg-[#0c0c0d] text-[#f5f5f7] animate-in fade-in duration-500">
        <div className="max-w-5xl mx-auto py-6 px-8 space-y-8">
          {groups.map((group) => (
            <section key={group.statusId} className="space-y-2">
              {/* Header - Linear Style */}
              <div className="flex items-center gap-2 px-2 py-1">
                <div
                  className="w-2.5 h-2.5 rounded-full"
                  style={{ backgroundColor: group.color }}
                />
                <h3 className="text-xs font-semibold text-[#8a8b94]">
                  {group.statusName}
                </h3>
                <span className="text-xs text-[#4a4b53]">
                  {group.tasks.length + group.folders.length}
                </span>
              </div>

              {/* Table/List Container */}
              <div className="border border-[#202127] rounded-lg bg-[#141416] overflow-hidden shadow-sm">
                <SortableContext items={[...group.folders.map(f => f.id), ...group.tasks.map(t => t.id)]} strategy={verticalListSortingStrategy}>
                  {group.folders.map((folder: any, idx: number) => (
                    <SortableItem key={folder.id} id={folder.id}>
                      <FolderListRow 
                        folder={folder} 
                        isFirst={idx === 0}
                        onClick={() => handleFolderClick(folder.id)}
                      />
                    </SortableItem>
                  ))}
                  {group.tasks.map((task: any, idx: number) => (
                    <SortableItem key={task.id} id={task.id}>
                      <TaskListRow 
                        task={task} 
                        isFirst={group.folders.length === 0 && idx === 0}
                        onClick={() => handleTaskClick(task.id)}
                      />
                    </SortableItem>
                  ))}
                </SortableContext>
                
                {group.tasks.length === 0 && group.folders.length === 0 && (
                  <div className="h-12 flex items-center justify-center text-xs text-[#4a4b53]">
                     No items in this section
                  </div>
                )}
              </div>
            </section>
          ))}
        </div>
      </div>
      <DragOverlay>
        {activeFolder ? <FolderListRow folder={activeFolder} onClick={() => {}} /> : null}
        {activeTask ? <TaskListRow task={activeTask} onClick={() => {}} /> : null}
      </DragOverlay>
    </DndContext>
  );
}

function TaskListRow({ task, onClick, isFirst }: { task: any; onClick: () => void; isFirst?: boolean }) {
  const displayId = `T-${task.id.slice(0, 4).toUpperCase()}`;
  
  const getPriorityIcon = (priority: string) => {
    switch (priority) {
      case "High": return <SignalHigh className="h-3.5 w-3.5 text-[#f5f5f7]" />;
      case "Normal": return <SignalMedium className="h-3.5 w-3.5 text-[#8a8b94]" />;
      case "Low": return <SignalLow className="h-3.5 w-3.5 text-[#4a4b53]" />;
      case "Urgent": return <AlertTriangle className="h-3.5 w-3.5 text-[#e5484d]" />;
      default: return <Circle className="h-3.5 w-3.5 text-[#4a4b53]" />;
    }
  };

  return (
    <div 
      onClick={onClick}
      className={cn(
        "h-10 px-4 flex items-center gap-4 hover:bg-[#1c1c1f] transition-colors cursor-pointer border-t border-[#202127]",
        isFirst && "border-t-0"
      )}
    >
      <div className="flex items-center gap-3 shrink-0">
         {getPriorityIcon(task.priority)}
         <span className="text-xs font-medium text-[#4a4b53] tracking-wider min-w-[50px]">
            {""}
         </span>
      </div>

      <div className="flex-1 min-w-0">
         <span className="text-sm font-medium text-[#f5f5f7] truncate block">
           {task.name}
         </span>
      </div>

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

function FolderListRow({ folder, onClick, isFirst }: { folder: any; onClick: () => void; isFirst?: boolean }) {
  const folderColor = folder.color || "#3b82f6";

  return (
    <div 
      onClick={onClick}
      className={cn(
        "h-10 px-4 flex items-center gap-4 hover:bg-[#1c1c1f] transition-colors cursor-pointer border-t border-[#202127] bg-[#141416]",
        isFirst && "border-t-0"
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
              color: folderColor 
            }}
         >
            {folder.icon ? <span className="text-xs">{folder.icon}</span> : <FolderIcon className="h-3 w-3" />}
         </div>
         <span className="text-sm font-bold text-[#f5f5f7] truncate block">
           {folder.name}
         </span>
      </div>

      <div className="flex items-center gap-4 shrink-0">
         <div className="flex items-center gap-2">
            <div className="h-1 w-16 bg-[#202127] rounded-full overflow-hidden">
               <div className="h-full bg-[#3b82f6]/40 w-1/3 rounded-full" />
            </div>
            <span className="text-xs font-medium text-[#4a4b53]">35%</span>
         </div>
      </div>
    </div>
  );
}

function SortableItem({ id, children }: { id: string; children: React.ReactNode }) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <div ref={setNodeRef} style={style} {...attributes} {...listeners}>
      {children}
    </div>
  );
}
