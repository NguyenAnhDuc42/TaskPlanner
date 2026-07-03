import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import {
  EntityLayerType as EntityLayerConst,
} from "@/types/entity-layer-type";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";

import { Plus, ChevronDown } from "lucide-react";
import { DndContext, DragOverlay } from "@dnd-kit/core";
import { restrictToVerticalAxis } from "@dnd-kit/modifiers";
import { pointerAwareCollisionDetection } from "@/lib/dnd-collision";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import { cn } from "@/lib/utils";
import { useState } from "react";

// Modularized Components & Hooks
import { useHierarchyDnd } from "./dnd/use-hierarchy-dnd";
import { SpaceNodeItem } from "./items/space-node-item";
import { FolderNodeItem } from "./items/folder-node-item";
import { TaskNodeItem } from "./items/task-node-item";
import { CreateSpaceForm } from "../../components/forms/create-space-form";
import { SpaceNodeList } from "./items/space-node-list";
import { FavoriteNodeList } from "./items/favorite-node-list";
export function HierarchySidebar() {
  const [isHierarchyOpen, setIsHierarchyOpen] = useState(true);
  const [isFavoritesOpen, setIsFavoritesOpen] = useState(true);
  const [isHeaderCreateOpen, setIsHeaderCreateOpen] = useState(false);
  const [isInlineCreateOpen, setIsInlineCreateOpen] = useState(false);
  const { canCreateSpace } = useWorkspaceRole();

  const { sensors, handleDragStart, handleDragEnd, activeItem } =
    useHierarchyDnd();

  return (
    <div className="h-full flex flex-col bg-transparent overflow-hidden select-none">
      {/* NAVIGATION SECTION (PANE 1) */}
      <div
        className={cn(
          "flex flex-col min-h-0 border-b border-border/40 overflow-hidden",
          isHierarchyOpen ? "flex-1" : "flex-none",
        )}
      >
        <Collapsible
          open={isHierarchyOpen}
          onOpenChange={setIsHierarchyOpen}
          className="flex flex-col h-full overflow-hidden"
        >
          <div className="w-full h-7 flex items-center gap-2 px-1 flex-none bg-muted/30 sticky top-0 z-10 group hover:bg-muted/60 transition-colors">
            <CollapsibleTrigger className="flex items-center gap-2 flex-1 min-w-0">
              <ChevronDown
                className={cn(
                  "h-3 w-3 text-muted-foreground transition-transform duration-200",
                  !isHierarchyOpen && "-rotate-90",
                )}
              />
              <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider text-left">
                Items
              </span>
            </CollapsibleTrigger>
            {canCreateSpace && (
              <DialogFormWrapper
                title="Create New Space"
                open={isHeaderCreateOpen}
                onOpenChange={setIsHeaderCreateOpen}
                trigger={
                  <Plus
                    className="h-3 w-3 text-muted-foreground opacity-0 group-hover:opacity-100 transition-none hover:text-primary cursor-pointer"
                    onClick={() => setIsHeaderCreateOpen(true)}
                  />
                }
              >
                <CreateSpaceForm onCancel={() => setIsHeaderCreateOpen(false)} />
              </DialogFormWrapper>
            )}
          </div>

          <CollapsibleContent className="flex-1 min-h-0 overflow-hidden data-[state=open]:flex data-[state=closed]:hidden">
            <div className="h-full w-full overflow-y-auto overflow-x-auto [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar]:h-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/40 [&::-webkit-scrollbar-track]:bg-transparent">
              <div className="px-1 pt-0.5 pb-2 flex flex-col min-w-fit">
                <DndContext
                  sensors={sensors}
                  collisionDetection={pointerAwareCollisionDetection}
                  onDragStart={handleDragStart}
                  onDragEnd={handleDragEnd}
                  modifiers={[restrictToVerticalAxis]}
                >
                  <SpaceNodeList />

                  {canCreateSpace && (
                    <div className="mb-px">
                      <DialogFormWrapper
                        title="Create New Space"
                        open={isInlineCreateOpen}
                        onOpenChange={setIsInlineCreateOpen}
                        trigger={
                          <button
                            className="flex items-center w-full px-1 py-0.5 rounded-md transition-colors cursor-pointer hover:bg-muted text-muted-foreground/50 hover:text-primary group"
                            onClick={() => setIsInlineCreateOpen(true)}
                          >
                            <div className="w-5 h-5 flex items-center justify-center shrink-0 mr-0.5">
                              <Plus className="h-3.5 w-3.5" />
                            </div>
                            <span className="text-[10px] font-bold uppercase tracking-widest">
                              Add Item
                            </span>
                          </button>
                        }
                      >
                        <CreateSpaceForm onCancel={() => setIsInlineCreateOpen(false)} />
                      </DialogFormWrapper>
                    </div>
                  )}

                  <DragOverlay adjustScale={false} zIndex={1000}>
                    {activeItem ? (
                      <div className="opacity-80 scale-105 transition-transform pointer-events-none shadow-2xl rounded-sm overflow-hidden ring-1 ring-primary/20">
                        {activeItem.type === EntityLayerConst.ProjectSpace && (
                          <SpaceNodeItem
                            spaceId={activeItem.id}
                            isForcedOpen={false}
                          />
                        )}
                        {activeItem.type === EntityLayerConst.ProjectFolder && (
                          <FolderNodeItem
                            folderId={activeItem.id}
                            spaceId={activeItem.spaceId}
                          />
                        )}
                        {activeItem.type === EntityLayerConst.ProjectTask && (
                          <TaskNodeItem
                            taskId={activeItem.id}
                            parentId={activeItem.parentId}
                            parentType={activeItem.parentType}
                            spaceId={activeItem.spaceId}
                          />
                        )}
                      </div>
                    ) : null}
                  </DragOverlay>
                </DndContext>
              </div>
            </div>
          </CollapsibleContent>
        </Collapsible>
      </div>

      {/* FAVORITES SECTION */}
      <div className="flex flex-col min-h-0 flex-none border-b border-border/40">
        <Collapsible open={isFavoritesOpen} onOpenChange={setIsFavoritesOpen} className="flex flex-col overflow-hidden">
          <div className="w-full h-7 flex items-center gap-2 px-1 flex-none bg-muted/30 sticky top-0 z-10 hover:bg-muted/60 transition-colors">
            <CollapsibleTrigger className="flex items-center gap-2 flex-1 min-w-0">
              <ChevronDown className={cn("h-3 w-3 text-muted-foreground transition-transform duration-200", !isFavoritesOpen && "-rotate-90")} />
              <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider text-left">
                Favorites
              </span>
            </CollapsibleTrigger>
          </div>
          <CollapsibleContent className="overflow-hidden data-[state=open]:block data-[state=closed]:hidden">
            <div className="px-1 pt-0.5 pb-2 flex flex-col max-h-48 overflow-y-auto [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
              <FavoriteNodeList />
            </div>
          </CollapsibleContent>
        </Collapsible>
      </div>

    </div>
  );
}
