import { DndContext, type DragEndEvent, PointerSensor, useSensor, useSensors, DragOverlay } from "@dnd-kit/core";
import { SortableContext, verticalListSortingStrategy, useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { useMemo, useState, useEffect } from "react";
import { useNavigate, useParams } from "@tanstack/react-router";
import { cn } from "@/lib/utils";
import { format } from "date-fns";
import { Clock, User, SignalLow, SignalMedium, SignalHigh, AlertTriangle, Circle } from "lucide-react";
import type { TaskViewData } from "../../layer-detail-types";
import { useUpdateTask } from "../task/task-api";

interface FolderListViewProps {
  viewData: TaskViewData;
}

export function FolderListView({ viewData }: FolderListViewProps) {
  const navigate = useNavigate();
  const { workspaceId } = useParams({ strict: false }) as any;

  const [localTasks, setLocalTasks] = useState(viewData.tasks || []);
  const [activeId, setActiveId] = useState<string | null>(null);

  useEffect(() => {
    setLocalTasks(viewData.tasks || []);
  }, [viewData.tasks]);

  const groups = useMemo(() => {
    const { statuses = [] } = viewData;
    const statusIds = statuses.map((s: any) => s.statusId);

    const unclassifiedTasks = localTasks.filter((t: any) => !statusIds.includes(t.statusId));

    const mappedGroups = statuses.map((status: any) => ({
      statusId: status.statusId,
      statusName: status.name,
      color: status.color,
      tasks: localTasks.filter((t: any) => t.statusId === status.statusId),
    }));

    if (unclassifiedTasks.length > 0) {
      mappedGroups.push({
        statusId: "unclassified",
        statusName: "Unclassified",
        color: "#4a4b53",
        tasks: unclassifiedTasks,
      });
    }

    return mappedGroups;
  }, [viewData.statuses, localTasks]);

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 8,
      },
    })
  );

  const { mutate: updateTask } = useUpdateTask();

  function handleDragStart(event: any) {
    setActiveId(event.active.id);
  }

  function handleDragEnd(event: DragEndEvent) {
    setActiveId(null);
    const { active, over } = event;
    if (!over) return;

    const taskId = active.id as string;
    let newStatusId = over.id as string;
    
    const overTask = localTasks.find(t => t.id === newStatusId);
    if (overTask) newStatusId = overTask.statusId || "unclassified";

    const finalStatusId = newStatusId === "unclassified" ? undefined : newStatusId;

    setLocalTasks(prev => prev.map(t => t.id === taskId ? { ...t, statusId: finalStatusId } : t));
    updateTask({ taskId, statusId: finalStatusId });
  }

  const handleTaskClick = (taskId: string) => {
    navigate({ to: "/workspaces/$workspaceId/tasks/$taskId", params: { workspaceId, taskId } });
  };

  const activeTask = localTasks.find(t => t.id === activeId);

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
                  {group.tasks.length}
                </span>
              </div>

              {/* Table/List Container */}
              <div className="border border-[#202127] rounded-lg bg-[#141416] overflow-hidden shadow-sm">
                <SortableContext items={group.tasks.map(t => t.id)} strategy={verticalListSortingStrategy}>
                  {group.tasks.map((task: any, idx: number) => (
                    <SortableItem key={task.id} id={task.id}>
                      <TaskListRow 
                        task={task} 
                        isFirst={idx === 0}
                        onClick={() => handleTaskClick(task.id)}
                      />
                    </SortableItem>
                  ))}
                </SortableContext>
                
                {group.tasks.length === 0 && (
                  <div className="h-12 flex items-center justify-center text-xs text-[#4a4b53]">
                     No tasks in this section
                  </div>
                )}
              </div>
            </section>
          ))}
        </div>
      </div>
      <DragOverlay>
        {activeTask ? <TaskListRow task={activeTask} onClick={() => {}} /> : null}
      </DragOverlay>
    </DndContext>
  );
}

function TaskListRow({ task, onClick, isFirst }: { task: any; onClick: () => void; isFirst?: boolean }) {
  const displayId = `T-${task.id.slice(0, 4).toUpperCase()}`;
  
  // Priority Icon Mapping
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
      {/* Priority & ID */}
      <div className="flex items-center gap-3 shrink-0">
         {getPriorityIcon(task.priority)}
         <span className="text-xs font-medium text-[#4a4b53] tracking-wider min-w-[50px]">
            {""}
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
