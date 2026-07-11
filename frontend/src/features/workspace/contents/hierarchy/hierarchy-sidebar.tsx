import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import {
  EntityLayerType as EntityLayerConst,
} from "@/types/entity-layer-type";

import { Plus, ChevronDown, LayoutGrid } from "lucide-react";
import { DndContext, DragOverlay } from "@dnd-kit/core";
import { restrictToVerticalAxis } from "@dnd-kit/modifiers";
import { pointerAwareCollisionDetection } from "@/lib/dnd-collision";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import { cn } from "@/lib/utils";
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuTrigger,
} from "@/components/ui/context-menu";
import { useEffect, useMemo, useRef, useState } from "react";
import { toast } from "sonner";
import { extractErrorMessage } from "@/types/api-error";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { SpaceMutations } from "@/mutations/space.mutations";

// Modularized Components & Hooks
import { useHierarchyDnd } from "./dnd/use-hierarchy-dnd";
import { SpaceNodeItem } from "./items/space-node-item";
import { FolderNodeItem } from "./items/folder-node-item";
import { TaskNodeItem } from "./items/task-node-item";
import { SpaceNodeList } from "./items/space-node-list";
import { FavoriteNodeList } from "./items/favorite-node-list";
export function HierarchySidebar() {
  const [isHierarchyOpen, setIsHierarchyOpen] = useState(true);
  const [isFavoritesOpen, setIsFavoritesOpen] = useState(true);
  const [isCreatingSpace, setIsCreatingSpace] = useState(false);
  const [newSpaceName, setNewSpaceName] = useState("");
  const spaceInputRef = useRef<HTMLInputElement>(null);
  const submittedRef = useRef(false);
  const { canCreateSpace } = useWorkspaceRole();
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const spaceMutations = useMemo(() => new SpaceMutations(rootStore, syncEngine), [rootStore, syncEngine]);

  useEffect(() => {
    if (!isCreatingSpace) return;
    // The row can mount while the Items Collapsible is still animating open — a synchronous
    // focus() call right after mount can land before the input is actually focusable yet. Double
    // rAF waits for that layout/paint to settle first.
    let raf2 = 0;
    const raf1 = requestAnimationFrame(() => {
      raf2 = requestAnimationFrame(() => spaceInputRef.current?.focus());
    });
    return () => { cancelAnimationFrame(raf1); cancelAnimationFrame(raf2); };
  }, [isCreatingSpace]);

  const handleCreateSpace = () => {
    if (submittedRef.current) return;
    submittedRef.current = true;
    const name = newSpaceName.trim();
    setIsCreatingSpace(false);
    setNewSpaceName("");
    if (name) {
      spaceMutations.create({ name, isPrivate: false, color: "#6366f1", icon: "LayoutGrid" })
        .catch((err) => toast.error(extractErrorMessage(err, "Failed to create space")));
    }
    setTimeout(() => { submittedRef.current = false; }, 300);
  };

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
              <Plus
                className="h-3 w-3 mr-1.5 text-muted-foreground opacity-0 group-hover:opacity-100 transition-none hover:text-primary cursor-pointer"
                onClick={(e) => {
                  e.stopPropagation();
                  setIsHierarchyOpen(true);
                  setIsCreatingSpace(true);
                }}
              />
            )}
          </div>

          <CollapsibleContent className="flex-1 min-h-0 overflow-hidden data-[state=open]:flex data-[state=closed]:hidden">
            <ContextMenu>
              <ContextMenuTrigger asChild>
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
                      {isCreatingSpace ? (
                        <div className="flex items-center gap-1.5 px-1 py-0.5 rounded-md border border-primary/40 bg-primary/5">
                          <div className="w-5 h-5 flex items-center justify-center shrink-0">
                            <LayoutGrid className="h-3.5 w-3.5" color="#6366f1" />
                          </div>
                          <input
                            ref={spaceInputRef}
                            type="text"
                            value={newSpaceName}
                            onChange={(e) => setNewSpaceName(e.target.value)}
                            onKeyDown={(e) => {
                              if (e.key === "Enter") e.currentTarget.blur();
                              if (e.key === "Escape") { setIsCreatingSpace(false); setNewSpaceName(""); }
                            }}
                            onBlur={handleCreateSpace}
                            placeholder="Space name..."
                            className="flex-1 text-[11px] font-semibold bg-transparent border-none outline-none text-foreground placeholder:text-muted-foreground/40"
                          />
                        </div>
                      ) : (
                        <button
                          className="flex items-center w-full px-1 py-0.5 rounded-md transition-colors cursor-pointer hover:bg-muted text-muted-foreground/50 hover:text-primary group"
                          onClick={() => setIsCreatingSpace(true)}
                        >
                          <div className="w-5 h-5 flex items-center justify-center shrink-0 mr-0.5">
                            <Plus className="h-3.5 w-3.5" />
                          </div>
                          <span className="text-[10px] font-bold uppercase tracking-widest">
                            Add Item
                          </span>
                        </button>
                      )}
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
              </ContextMenuTrigger>
              <ContextMenuContent className="w-44 bg-popover text-popover-foreground border-border/50 shadow-2xl rounded-xl p-1.5">
                {canCreateSpace && (
                  <ContextMenuItem
                    className="gap-2 cursor-pointer"
                    onSelect={() => { setIsHierarchyOpen(true); setIsCreatingSpace(true); }}
                  >
                    <Plus className="h-3.5 w-3.5" />
                    <span>Create Space</span>
                  </ContextMenuItem>
                )}
              </ContextMenuContent>
            </ContextMenu>
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
