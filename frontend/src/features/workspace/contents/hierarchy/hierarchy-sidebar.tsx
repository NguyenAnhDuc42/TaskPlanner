import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { useHierarchy, useMoveItem, } from "./hierarchy-api";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { SpaceForm } from "./hierarchy-components/creation-form/space-form";
import { Loader2, Plus, Search, ChevronDown, FileText, Clock } from "lucide-react";
import { 
  DndContext, 
  closestCenter,
  DragOverlay,
} from "@dnd-kit/core";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";
import { restrictToVerticalAxis } from "@dnd-kit/modifiers";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import { cn } from "@/lib/utils";
import { ScrollArea } from "@/components/ui/scroll-area";
import { useState, useMemo, useDeferredValue } from "react";
import type { SpaceHierarchy, FolderHierarchy, TaskHierarchy,} from "./hierarchy-type";

// Modularized Components & Hooks
import { useHierarchyDnd } from "./dnd/use-hierarchy-dnd";
import { SpaceItem } from "./items/space-item";
import { FolderItem } from "./items/folder-item";
import { TaskItem } from "./items/task-item";
import { MockItem } from "./items/mock-item";

export function HierarchySidebar() {
  const { workspaceId } = useWorkspace();
  const { data: hierarchy, isLoading, error } = useHierarchy(workspaceId);
  const [isHierarchyOpen, setIsHierarchyOpen] = useState(true);
  const [isDocsOpen, setIsDocsOpen] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");
  const deferredSearchQuery = useDeferredValue(searchQuery);
  
  const moveItem = useMoveItem(workspaceId || "");

  const filteredHierarchy = useMemo(() => {
    if (!hierarchy || !deferredSearchQuery) return hierarchy;
    const query = deferredSearchQuery.toLowerCase();
    const filteredSpaces = hierarchy.spaces
      .map((space) => {
        const folders = space.folders.filter((f) =>
          f.name.toLowerCase().includes(query),
        );
        const isSpaceMatch = space.name.toLowerCase().includes(query);
        if (isSpaceMatch || folders.length > 0) {
          return { ...space, folders, tasks: [], isExpanded: true };
        }
        return null;
      })
      .filter((s) => s !== null) as SpaceHierarchy[];
    return { ...hierarchy, spaces: filteredSpaces };
  }, [hierarchy, deferredSearchQuery]);

  const { sensors, handleDragStart, handleDragEnd, activeItem } = useHierarchyDnd({ 
    workspaceId: workspaceId || "",
    filteredHierarchy, 
    moveItem 
  });

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center h-40 gap-3 text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        <span className="text-[10px] font-bold uppercase tracking-widest">Loading...</span>
      </div>
    );
  }

  const hasData = !isLoading && !error && filteredHierarchy?.spaces && filteredHierarchy.spaces.length > 0;
  const showEmptyState = !hasData;

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
            <button onClick={() => setSearchQuery("")} className="text-xs text-muted-foreground hover:text-foreground transition-colors">✕</button>
          )}
        </div>
        <div className="flex-shrink-0 flex items-center justify-center p-1 border-l border-border">
          <DialogFormWrapper
            title="Create New Space"
            trigger={<Plus className="h-4 w-4 text-muted-foreground hover:text-foreground cursor-pointer" />}
            contentClassName="sm:max-w-[800px] p-0 overflow-hidden border-none shadow-2xl rounded-2xl bg-background outline-none ring-1 ring-border/50"
          >
            <SpaceForm workspaceId={workspaceId || ""} onSubmitSuccess={() => {}} onCancel={() => {}} />
          </DialogFormWrapper>
        </div>
      </div>

      {/* NAVIGATION SECTION (PANE 1) */}
      <div className={cn(
        "flex flex-col min-h-0 transition-all duration-200 border-b border-border/40 overflow-hidden",
        isHierarchyOpen ? "flex-1" : "flex-none"
      )}>
        <Collapsible 
          open={isHierarchyOpen} 
          onOpenChange={setIsHierarchyOpen}
          className="flex flex-col h-full overflow-hidden"
        >
          <CollapsibleTrigger 
            className="w-full flex items-center gap-2 px-1 py-1.5 hover:bg-muted/50 transition-colors group flex-none bg-background sticky top-0 z-10"
          >
            <ChevronDown className={cn("h-3 w-3 text-muted-foreground transition-transform duration-200", !isHierarchyOpen && "-rotate-90")} />
            <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider flex-1 text-left">Navigation</span>
            <div onClick={(e) => e.stopPropagation()}>
              <DialogFormWrapper
                title="Create New Space"
                trigger={<Plus className="h-3 w-3 text-muted-foreground opacity-0 group-hover:opacity-100 transition-none hover:text-primary cursor-pointer" />}
                contentClassName="sm:max-w-[800px] p-0 overflow-hidden border-none shadow-2xl rounded-2xl bg-background outline-none ring-1 ring-border/50"
              >
                <SpaceForm workspaceId={workspaceId || ""} onSubmitSuccess={() => {}} onCancel={() => {}} />
              </DialogFormWrapper>
            </div>
          </CollapsibleTrigger>
          
          <CollapsibleContent className="flex-1 min-h-0 overflow-hidden data-[state=open]:flex data-[state=closed]:hidden">
            <ScrollArea className="h-full w-full">
              <div className="px-1 pt-0.5 pb-2 flex flex-col">
                <DndContext 
                  sensors={sensors} 
                  collisionDetection={closestCenter} 
                  onDragStart={handleDragStart}
                  onDragEnd={handleDragEnd} 
                  modifiers={[restrictToVerticalAxis]}
                >
                  {!showEmptyState && filteredHierarchy?.spaces && (
                    <SortableContext items={filteredHierarchy.spaces.map(s => `space-${s.id}`)} strategy={verticalListSortingStrategy}>
                      {filteredHierarchy.spaces.map((space) => (
                        <SpaceItem key={space.id} space={space} isForcedOpen={!!deferredSearchQuery} />
                      ))}
                    </SortableContext>
                  )}

                  {!searchQuery && (
                    <div className="mb-px">
                      <DialogFormWrapper
                        title="Create New Space"
                        trigger={
                          <button className="flex items-center w-full px-1 py-0.5 rounded-sm transition-colors cursor-pointer hover:bg-muted text-muted-foreground/50 hover:text-primary group">
                            <div className="w-5 h-5 flex items-center justify-center flex-shrink-0 mr-0.5">
                              <Plus className="h-3.5 w-3.5" />
                            </div>
                            <span className="text-[10px] font-bold uppercase tracking-widest">Add Item</span>
                          </button>
                        }
                        contentClassName="sm:max-w-[800px] p-0 overflow-hidden border-none shadow-2xl rounded-2xl bg-background outline-none ring-1 ring-border/50"
                      >
                        <SpaceForm workspaceId={workspaceId || ""} onSubmitSuccess={() => {}} onCancel={() => {}} />
                      </DialogFormWrapper>
                    </div>
                  )}

                  <DragOverlay adjustScale={false} zIndex={1000}>
                    {activeItem ? (
                      <div className="opacity-80 scale-105 transition-transform pointer-events-none shadow-2xl rounded-sm overflow-hidden ring-1 ring-primary/20">
                        {activeItem.type === EntityLayerConst.ProjectSpace && (
                          <SpaceItem space={activeItem.data as SpaceHierarchy} isForcedOpen={false} />
                        )}
                        {activeItem.type === EntityLayerConst.ProjectFolder && (
                          <FolderItem folder={activeItem.data as FolderHierarchy} spaceId={(activeItem.data as any).spaceId || ""} />
                        )}
                        {activeItem.type === EntityLayerConst.ProjectTask && (
                          <TaskItem task={activeItem.data as TaskHierarchy} parentId={activeItem.data.parentId || ""} parentType={activeItem.data.parentType as EntityLayerConst || EntityLayerConst.ProjectFolder} spaceId={(activeItem.data as any).spaceId || ""} />
                        )}
                      </div>
                    ) : null}
                  </DragOverlay>
                </DndContext>
              </div>
            </ScrollArea>
          </CollapsibleContent>
        </Collapsible>
      </div>

      {/* DOCS & TASKS SECTION (PANE 2) */}
      <div className={cn(
        "flex flex-col min-h-0 transition-all duration-200 overflow-hidden bg-background",
        isDocsOpen ? "flex-none max-h-[300px]" : "flex-none"
      )}>
        <Collapsible 
          open={isDocsOpen} 
          onOpenChange={setIsDocsOpen}
          className="flex flex-col h-full overflow-hidden"
        >
          <CollapsibleTrigger 
            className="w-full flex items-center gap-2 px-1 py-1.5 hover:bg-muted/50 transition-colors group flex-none bg-background sticky top-0 z-10"
          >
            <ChevronDown className={cn("h-3 w-3 text-muted-foreground transition-transform duration-200", !isDocsOpen && "-rotate-90")} />
            <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider text-left">Docs & Tasks</span>
          </CollapsibleTrigger>
          
          <CollapsibleContent className="flex-1 min-h-0 overflow-hidden data-[state=open]:flex data-[state=closed]:hidden">
            <ScrollArea className="h-full w-full">
              <div className="px-2 pt-0.5 pb-2 flex flex-col">
                <MockItem icon={FileText} label="Product Roadmap" count={3} />
                <MockItem icon={FileText} label="Design Systems" count={12} />
                <MockItem icon={Clock} label="Recent Tasks" />
              </div>
            </ScrollArea>
          </CollapsibleContent>
        </Collapsible>
      </div>
    </div>
  );
}
