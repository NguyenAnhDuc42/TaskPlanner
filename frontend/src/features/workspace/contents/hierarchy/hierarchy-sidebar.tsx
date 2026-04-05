import { useSidebarContext } from "@/features/workspace/components/sidebar-provider";
import { useHierarchy, useNodeTasks, useMoveItem } from "./hierarchy-api";

import {
  Loader2,
  ChevronRight,
  Plus,
  Lock,
  CheckSquare,
  Search,
  FileText,
  Clock,
  ChevronDown,
  MoreHorizontal,
} from "lucide-react";
import * as Icons from "lucide-react";
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
  useSortable,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { restrictToVerticalAxis } from "@dnd-kit/modifiers";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import { cn } from "@/lib/utils";
import { ScrollArea } from "@/components/ui/scroll-area";
import { useNavigate, useLocation } from "@tanstack/react-router";
import { useState, useMemo } from "react";
import type {
  SpaceHierarchy,
  FolderHierarchy,
  TaskHierarchy,
} from "./hierarchy-type";
import { DropdownWrapper } from "@/components/dropdown-wrapper";
import { SpaceMenu } from "./hierarchy-components/dropdown/space-menu";
import { FolderMenu } from "./hierarchy-components/dropdown/folder-menu";
import { TaskMenu } from "./hierarchy-components/dropdown/task-menu";

const NAME_CHAR_LIMIT = 20;

function clampName(name: string, limit = NAME_CHAR_LIMIT) {
  if (name.length <= limit) return name;
  return `${name.slice(0, Math.max(0, limit - 1))}…`;
}

export function HierarchySidebar() {
  const { workspaceId } = useSidebarContext();
  const { data: hierarchy, isLoading, error } = useHierarchy(workspaceId || "");
  const [isHierarchyOpen, setIsHierarchyOpen] = useState(true);
  const [isDocsOpen, setIsDocsOpen] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");
  const moveItem = useMoveItem(workspaceId || "");

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: { delay: 150, tolerance: 5 }, // Allow normal clicks
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;

    const activeType = active.data.current?.type;
    const overType = over.data.current?.type;

    if (activeType !== overType) return;

    const itemId = active.data.current?.id;
    const itemType = activeType as "Space" | "Folder" | "Task";
    
    // Logic to find neighbors based on type
    let prevKey: string | undefined;
    let nextKey: string | undefined;
    let targetParentId: string | undefined;

    if (itemType === "Space") {
      const spaces = filteredHierarchy?.spaces || [];
      const overIndex = spaces.findIndex(s => s.id === over.data.current?.id);
      const activeIndex = spaces.findIndex(s => s.id === itemId);
      
      const newIndex = overIndex;
      prevKey = newIndex > 0 ? (newIndex < activeIndex ? spaces[newIndex - 1].orderKey : spaces[newIndex].orderKey) : undefined;
      nextKey = newIndex < spaces.length - 1 ? (newIndex > activeIndex ? spaces[newIndex + 1].orderKey : spaces[newIndex].orderKey) : undefined;
      
      // Simpler logic: just find the neighbours in the resulting array
      const movedSpaces = [...spaces];
      const [removed] = movedSpaces.splice(activeIndex, 1);
      movedSpaces.splice(overIndex, 0, removed);
      
      const finalIndex = overIndex;
      prevKey = finalIndex > 0 ? movedSpaces[finalIndex - 1].orderKey : undefined;
      nextKey = finalIndex < movedSpaces.length - 1 ? movedSpaces[finalIndex + 1].orderKey : undefined;
    } 
    else if (itemType === "Folder") {
      // Find which space the 'over' folder belongs to
      const targetSpace = filteredHierarchy?.spaces.find(s => s.folders.some(f => f.id === over.data.current?.id));
      if (!targetSpace) return;
      
      targetParentId = targetSpace.id;
      const folders = targetSpace.folders;
      const overIndex = folders.findIndex(f => f.id === over.data.current?.id);
      const activeIndex = folders.findIndex(f => f.id === itemId);
      
      const movedFolders = [...folders];
      if (activeIndex !== -1) {
        const [removed] = movedFolders.splice(activeIndex, 1);
        movedFolders.splice(overIndex, 0, removed);
      } else {
        // Dragging from another space
        movedFolders.splice(overIndex, 0, { id: itemId } as any);
      }
      
      const finalIndex = overIndex;
      prevKey = finalIndex > 0 ? movedFolders[finalIndex - 1].orderKey : undefined;
      nextKey = finalIndex < movedFolders.length - 1 ? movedFolders[finalIndex + 1].orderKey : undefined;
    }
    else if (itemType === "Task") {
      // For tasks, we handle it similarly but we might not have the full list if it's infinite scroll
      // Backend handles "null" neighbours as "move to end" or "move to start"
      // We use the over item's orderKey and its neighbour if available
      nextKey = over.data.current?.orderKey;
      // This is an approximation; backend midpoint still works if only one is provided
    }

    moveItem.mutate({
      itemId,
      itemType,
      targetParentId,
      previousItemOrderKey: prevKey,
      nextItemOrderKey: nextKey
    });
  };

  const filteredHierarchy = useMemo(() => {
    if (!hierarchy || !searchQuery) return hierarchy;
    const query = searchQuery.toLowerCase();
    const filteredSpaces = hierarchy.spaces
      .map((space) => {
        const folders = space.folders.filter((f) =>
          f.name.toLowerCase().includes(query),
        );
        const isSpaceMatch = space.name.toLowerCase().includes(query);
        // Only matching spaces or folders here, tasks are lazy-loaded
        if (isSpaceMatch || folders.length > 0) {
          return { ...space, folders, tasks: [], isExpanded: true };
        }
        return null;
      })
      .filter((s) => s !== null) as SpaceHierarchy[];
    return { ...hierarchy, spaces: filteredSpaces };
  }, [hierarchy, searchQuery]);

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center h-40 gap-3 text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        <span className="text-[10px] font-bold uppercase tracking-widest">
          Loading...
        </span>
      </div>
    );
  }

  if (error) return null;

  return (
    <div className="h-full flex flex-col bg-background overflow-hidden select-none">
      {/* Search & Actions */}
      <div className="px-1 pb-1 pt-0 border-b border-border flex-shrink-0 flex items-center gap-1">
        <div className="flex items-center gap-2 px-2 h-7 rounded-sm bg-muted border border-border focus-within:border-primary/60 transition-colors group flex-1">
          <Search className="h-3 w-3 text-muted-foreground flex-shrink-0" />
          <input
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Search..."
            className="flex-1 bg-transparent border-none outline-none text-[10px] font-semibold text-foreground placeholder:text-muted-foreground"
          />
          {searchQuery && (
            <button
              onClick={() => setSearchQuery("")}
              className="text-xs text-muted-foreground hover:text-foreground transition-colors"
            >
              ✕
            </button>
          )}
        </div>

        <div className="flex-shrink-0 flex items-center justify-center p-1 border-l border-border">
             <Plus className="h-4 w-4 text-muted-foreground hover:text-foreground cursor-pointer" />
        </div>
      </div>

      <ScrollArea className="flex-1 min-h-0">
        <div className="py-2">
          {/* NAVIGATION SECTION */}
          <Collapsible open={isHierarchyOpen} onOpenChange={setIsHierarchyOpen}>
            <CollapsibleTrigger className="w-full flex items-center gap-2 px-1 py-0.5 hover:bg-muted transition-colors group rounded-sm">
              <ChevronDown
                className={cn(
                  "h-3 w-3 text-muted-foreground transition-transform duration-200",
                  !isHierarchyOpen && "-rotate-90",
                )}
              />
              <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider flex-1 text-left">
                Navigation
              </span>
              <Plus 
                className="h-3 w-3 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity hover:text-primary cursor-pointer" 
              />
            </CollapsibleTrigger>
            <CollapsibleContent className="overflow-hidden data-[state=open]:animate-collapsible-down data-[state=closed]:animate-collapsible-up">
              <div className="max-h-[60vh] overflow-y-auto px-2 pt-0.5 pb-2 flex flex-col gap-0.5">
                <DndContext
                  sensors={sensors}
                  collisionDetection={closestCenter}
                  onDragEnd={handleDragEnd}
                  modifiers={[restrictToVerticalAxis]}
                >
                    <SortableContext
                    items={filteredHierarchy?.spaces.map(s => `space-${s.id}`) || []}
                    strategy={verticalListSortingStrategy}
                  >
                    {filteredHierarchy?.spaces.map((space) => (
                      <SpaceItem
                        key={space.id}
                        space={space}
                        isForcedOpen={!!searchQuery}
                      />
                    ))}
                  </SortableContext>
                </DndContext>
              </div>
            </CollapsibleContent>
          </Collapsible>

          {/* DOCS & TASKS SECTION */}
          <Collapsible open={isDocsOpen} onOpenChange={setIsDocsOpen}>
            <CollapsibleTrigger className="w-full flex items-center gap-2 px-1 py-0.5 hover:bg-muted transition-colors group mt-1 pt-1 border-t border-border rounded-sm">
              <ChevronDown
                className={cn(
                  "h-3 w-3 text-muted-foreground transition-transform duration-200",
                  !isDocsOpen && "-rotate-90",
                )}
              />
              <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider text-left">
                Docs & Tasks
              </span>
            </CollapsibleTrigger>
            <CollapsibleContent className="overflow-hidden data-[state=open]:animate-collapsible-down data-[state=closed]:animate-collapsible-up">
              <div className="px-2 pt-0.5 pb-2 flex flex-col gap-0.5">
                <MockItem icon={FileText} label="Product Roadmap" count={3} />
                <MockItem icon={FileText} label="Design Systems" count={12} />
                <MockItem icon={Clock} label="Recent Tasks" />
              </div>
            </CollapsibleContent>
          </Collapsible>
        </div>
      </ScrollArea>
    </div>
  );
}

function MockItem({
  icon: Icon,
  label,
  count,
}: {
  icon: any;
  label: string;
  count?: number;
}) {
  return (
    <div className="flex items-center gap-2 px-1 py-0.5 rounded-sm hover:bg-muted cursor-pointer group transition-colors">
      <Icon className="h-3.5 w-3.5 text-muted-foreground group-hover:text-foreground transition-colors flex-shrink-0" />
      <span className="text-[11px] font-semibold text-muted-foreground group-hover:text-foreground transition-colors flex-1 truncate">
        {label}
      </span>
      {count !== undefined && (
        <span className="text-[10px] font-mono text-muted-foreground">
          {count}
        </span>
      )}
    </div>
  );
}

function SpaceItem({space, isForcedOpen,}: {space: SpaceHierarchy; isForcedOpen?: boolean;}) {
  const [isOpen, setIsOpen] = useState(true);
  const { workspaceId } = useSidebarContext();
  const navigate = useNavigate();
  const location = useLocation();
  const isActive = location.pathname.includes(`/spaces/${space.id}`);
  const IconComponent = (Icons as any)[space.icon] || Icons.LayoutGrid;
  const spaceColor = space.color || "var(--primary)";
  const effectiveOpen = isForcedOpen || isOpen;

  return (
    <Collapsible open={effectiveOpen} onOpenChange={setIsOpen} className="w-full">
    <SortableItem id={`space-${space.id}`} data={{ type: 'Space', id: space.id, orderKey: space.orderKey }}>
      <div
        className={cn(
          "flex items-center w-full px-1 py-0.5 rounded-sm transition-colors cursor-pointer mb-px group",
          isActive
            ? "bg-primary/10 text-primary"
            : "text-foreground hover:bg-muted",
        )}
        onClick={() => navigate({ to: `/workspaces/${workspaceId}/spaces/${space.id}` })}
      >
        <div
          className="relative flex items-center justify-center w-5 h-5 flex-shrink-0 cursor-pointer rounded-sm hover:bg-background/50 group/icon mr-0.5"
          onClick={(e) => {e.stopPropagation(); setIsOpen(!isOpen);}}
        >
          <IconComponent
            className="h-3.5 w-3.5 absolute transition-opacity group-hover/icon:opacity-0"
            style={{ color: isActive ? spaceColor : undefined }}
          />
          <ChevronRight
            className={cn(
              "h-4 w-4 absolute opacity-0 transition-all text-muted-foreground group-hover/icon:opacity-100",
              isOpen && "rotate-90",
            )}
          />
        </div>
        <span className="truncate text-[11px] font-bold flex-1">
          {clampName(space.name)}
        </span>
        <div className="opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-1">
          <DropdownWrapper align="start" side="right" trigger={
              <button className="h-4 w-4 p-0.5 flex items-center justify-center rounded-sm hover:bg-muted-foreground/10 text-muted-foreground hover:text-primary transition-colors" onClick={(e) => e.stopPropagation()}>
                <MoreHorizontal className="h-3.5 w-3.5" />
              </button>
            }
          >
            <SpaceMenu spaceId={space.id} />
          </DropdownWrapper>
        </div>
        {space.isPrivate && (
          <Lock className="h-3 w-3 text-muted-foreground flex-shrink-0 opacity-40 ml-1" />
        )}
      </div>
    </SortableItem>

      <CollapsibleContent className="overflow-hidden data-[state=open]:animate-collapsible-down data-[state=closed]:animate-collapsible-up">
        <div className="ml-3 pl-1 border-l border-border mt-0.5 flex flex-col">
          {space.folders.map((folder) => (
            <FolderItem
              key={folder.id}
              folder={folder}
              spaceId={space.id}
            />
          ))}
          <NodeTasksList 
            workspaceId={workspaceId || ""} 
            nodeId={space.id} 
            parentType="Space" 
            isExpanded={effectiveOpen} 
          />
        </div>
      </CollapsibleContent>
    </Collapsible>
  );
}

function FolderItem({folder, spaceId,}: {folder: FolderHierarchy; spaceId: string;}) {
  const [isOpen, setIsOpen] = useState(false);
  const IconComponent = (Icons as any)[folder.icon] || Icons.Folder;
  const location = useLocation();
  const isActive = location.pathname.includes(`/folders/${folder.id}`);
  const navigate = useNavigate();
  const { workspaceId } = useSidebarContext();

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen} className="w-full">
    <SortableItem id={`folder-${folder.id}`} data={{ type: 'Folder', id: folder.id, orderKey: folder.orderKey, parentId: (folder as any).spaceId }}>
      <div
        className={cn(
          "flex items-center w-full px-1 py-0.5 rounded-sm transition-colors cursor-pointer mb-px group",
          isActive
            ? "bg-primary/10 text-foreground"
            : "text-muted-foreground hover:bg-muted hover:text-foreground",
        )}
        onClick={() => navigate({ to: `/workspaces/${workspaceId}/folders/${folder.id}` })}
      >
        <div className="relative flex items-center justify-center w-5 h-5 flex-shrink-0 cursor-pointer rounded-sm hover:bg-background/50 group/icon mr-0.5" onClick={(e) => {e.stopPropagation(); setIsOpen(!isOpen);}}>
          <IconComponent className="h-3.5 w-3.5 absolute transition-opacity group-hover/icon:opacity-0" style={{ color: folder.color }}/>
          <ChevronRight className={cn("h-4 w-4 absolute opacity-0 transition-all text-muted-foreground group-hover/icon:opacity-100", isOpen && "rotate-90")}/>
        </div>
        <span className="truncate text-[11px] font-semibold flex-1">
          {clampName(folder.name)}
        </span>
        <div className="opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-1">
          <DropdownWrapper align="start" side="right" trigger={
              <button className="h-4 w-4 p-0.5 flex items-center justify-center rounded-sm hover:bg-muted-foreground/10 text-muted-foreground hover:text-primary transition-colors" onClick={(e) => e.stopPropagation()}>
                <MoreHorizontal className="h-3.5 w-3.5" />
              </button>
            }
          >
            <FolderMenu folderId={folder.id} spaceId={spaceId} />
          </DropdownWrapper>
        </div>
      </div>
    </SortableItem>
      <CollapsibleContent className="overflow-hidden data-[state=open]:animate-collapsible-down data-[state=closed]:animate-collapsible-up">
        <div className="ml-3 pl-1 border-l border-border mt-0.5 flex flex-col">
          <NodeTasksList 
            workspaceId={workspaceId || ""} 
            nodeId={folder.id} 
            parentType="Folder" 
            isExpanded={isOpen} 
          />
        </div>
      </CollapsibleContent>
    </Collapsible>
  );
}

function NodeTasksList({workspaceId, nodeId, parentType, isExpanded,}: {workspaceId: string; nodeId: string; parentType: "Folder" | "Space"; isExpanded: boolean;}) {
  const {
    data,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isLoading,
  } = useNodeTasks(workspaceId, nodeId, parentType);

  if (!isExpanded) return null;

  if (isLoading) {
    return (
      <div className="flex items-center gap-2 pl-6 py-1 opacity-50">
        <Loader2 className="h-3 w-3 animate-spin" />
        <span className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">
          Loading Tasks...
        </span>
      </div>
    );
  }

  const allTasks = data?.pages.flatMap((page) => page.tasks) || [];

  return (
    <SortableContext items={allTasks.map(t => `task-${t.id}`)} strategy={verticalListSortingStrategy}>
      {allTasks.map((task) => (
        <TaskItem key={task.id} task={task} />
      ))}
      {hasNextPage && (
        <button
          onClick={(e) => {
            e.stopPropagation();
            fetchNextPage();
          }}
          disabled={isFetchingNextPage}
          className="flex items-center gap-2 pl-6 py-1 text-[10px] font-bold uppercase tracking-widest text-muted-foreground hover:text-primary transition-colors disabled:opacity-50"
        >
          {isFetchingNextPage ? (
            <Loader2 className="h-3 w-3 animate-spin" />
          ) : (
            <Plus className="h-3 w-3" />
          )}
          Load More
        </button>
      )}
      {allTasks.length === 0 && !isLoading && (
        <div className="pl-6 py-1 text-[10px] font-semibold text-muted-foreground/40 italic">
          No tasks
        </div>
      )}
    </SortableContext>
  );
}

function SortableItem({ id, data, children }: { id: string; data: any; children: React.ReactNode }) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging
  } = useSortable({ id, data });

  const style = {
    transform: CSS.Translate.toString(transform),
    transition,
    opacity: isDragging ? 0.6 : 1,
    scale: isDragging ? 0.98 : 1,
    zIndex: isDragging ? 50 : 1,
    position: 'relative' as const,
  };

  return (
    <div ref={setNodeRef} style={style} {...attributes} {...listeners}>
      {children}
    </div>
  );
}

function TaskItem({ task }: { task: TaskHierarchy }) {
  const navigate = useNavigate();
  const { workspaceId } = useSidebarContext();
  const location = useLocation();
  const isActive = location.pathname.includes(`/tasks/${task.id}`);

  return (
    <SortableItem id={`task-${task.id}`} data={{ type: 'Task', id: task.id, orderKey: task.orderKey }}>
      <div
        className={cn(
          "flex items-center w-full px-1 py-0.5 rounded-sm transition-colors cursor-pointer mb-px pl-6 group",
          isActive
            ? "text-primary bg-primary/10"
            : "text-muted-foreground hover:bg-muted hover:text-foreground",
        )}
        onClick={() =>
          navigate({ to: `/workspaces/${workspaceId}/tasks/${task.id}` })
        }
      >
        <CheckSquare className="h-3.5 w-3.5 flex-shrink-0 opacity-60 mr-1.5" />
        <span className="truncate text-[11px] font-semibold flex-1 leading-tight">
          {clampName(task.name)}
        </span>
        <div className="opacity-0 group-hover:opacity-100 transition-opacity">
          <DropdownWrapper align="start" side="right" trigger={
              <button className="h-4 w-4 p-0.5 flex items-center justify-center rounded-sm hover:bg-muted-foreground/10 text-muted-foreground hover:text-primary transition-colors" onClick={(e) => e.stopPropagation()}>
                <MoreHorizontal className="h-3.5 w-3.5" />
              </button>
            }
          >
            <TaskMenu taskId={task.id} />
          </DropdownWrapper>
        </div>
      </div>
    </SortableItem>
  );
}
