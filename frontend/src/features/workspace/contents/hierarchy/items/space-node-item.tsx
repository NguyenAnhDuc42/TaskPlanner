import { useState } from "react";
import { observer } from "mobx-react-lite";
import { ChevronRight, MoreVertical } from "lucide-react";
import { DynamicIcon } from "@/components/dynamic-icon";
import { useNavigate, useLocation } from "@tanstack/react-router";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { cn } from "@/lib/utils";
import { Collapsible, CollapsibleContent } from "@/components/ui/collapsible";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useLocalStorage } from "@/hooks/use-local-storage";
import { NodeTasksList } from "./task-node-list";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import { SortableItem } from "../dnd/sortable-item";
import { NodeFoldersList } from "./folder-node-list";
import { SpaceContextMenu } from "../hierarchy-components/context-menus/space-context-menu";
import { EntityMenuTrigger } from "../hierarchy-components/context-menus/shared";

interface SpaceNodeItemProps {
  spaceId: string;
  isForcedOpen?: boolean;
}

export const SpaceNodeItem = observer(function SpaceNodeItem({
  spaceId,
  isForcedOpen = false,
}: SpaceNodeItemProps) {
  const { workspaceId } = useWorkspace();
  const rootStore = useWorkspaceRootStore();

  const space = rootStore.spaceStore.getById(spaceId);
  const hasFolders = rootStore.folderStore.getBySpace(spaceId).length > 0;
  const hasTasks = rootStore.taskStore.all.some((t) => t.spaceId === spaceId && !t.folderId && !t.parentTaskId);
  const hasChildren = hasFolders || hasTasks;

  const navigate = useNavigate();
  const location = useLocation();
  const [isOpen, setIsOpen] = useLocalStorage(`sidebar-open:${workspaceId}:space:${spaceId}`, false);
  const [isCreatingFolder, setIsCreatingFolder] = useState(false);
  const [isCreatingTask, setIsCreatingTask] = useState(false);


  if (!space) return null;

  const isActive = location.pathname.includes(`/spaces/${space.id}`);
  const spaceColor = space.color || "var(--primary)";

  const effectiveOpen = isForcedOpen || isOpen;

  return (
    <Collapsible
      open={effectiveOpen}
      onOpenChange={setIsOpen}
      className="w-full"
    >
      <SortableItem
        id={`space-${space.id}`}
        data={{
          ...space,
          type: EntityLayerConst.ProjectSpace,
          id: space.id,
          orderKey: space.orderKey,
        }}
      >
        <SpaceContextMenu
          spaceId={space.id}
          spaceName={space.name}
          onCreateFolder={() => { setIsOpen(true); setIsCreatingFolder(true); }}
          onCreateTask={() => { setIsOpen(true); setIsCreatingTask(true); }}
        >
          <div
            className={cn(
              "flex items-center px-1 py-0.5 rounded-md transition-colors mb-px group border",
              isActive
                ? "bg-primary/5 text-primary border-primary/25"
                : "text-muted-foreground border-transparent hover:bg-muted/50 hover:text-foreground hover:border-border/30",
            )}
          >
            <button
              type="button"
              className="relative flex items-center justify-center w-5 h-5 shrink-0 cursor-pointer rounded-sm hover:bg-background/50 group/icon mr-1.5 transition-none"
              onClick={(e) => {
                if (!hasChildren) return;
                e.stopPropagation();
                setIsOpen(!isOpen);
              }}
            >
              <DynamicIcon
                name={space.icon || "LayoutGrid"}
                size={14}
                color={spaceColor}
                className={cn(
                  "absolute transition-none",
                  hasChildren && "group-hover/icon:opacity-0",
                )}
              />
              {hasChildren && (
                <ChevronRight
                  className={cn(
                    "h-4 w-4 absolute opacity-0 text-muted-foreground group-hover/icon:opacity-100 transition-none",
                    isOpen && "rotate-90",
                  )}
                />
              )}
            </button>

            <button
              type="button"
              className="flex-1 text-left text-[11px] font-bold outline-none select-none whitespace-nowrap"
              onClick={() => navigate({
                  to: "/workspaces/$workspaceId/spaces/$spaceId",
                  params: { workspaceId, spaceId: space.id },
                })}
            >
              {space.name}
            </button>

            <div className="flex items-center gap-0.5 ml-1 shrink-0">
              <EntityMenuTrigger>
                <button
                  type="button"
                  className="h-4 w-4 p-0.5 flex items-center justify-center rounded-sm hover:bg-muted-foreground/10 text-muted-foreground hover:text-primary transition-colors"
                  onClick={(e) => e.stopPropagation()}
                >
                  <MoreVertical className="h-3.5 w-3.5" />
                </button>
              </EntityMenuTrigger>
            </div>
          </div>
        </SpaceContextMenu>
      </SortableItem>

      <CollapsibleContent className="overflow-hidden">
        <div className="ml-3.25 pl-2 border-l border-border flex flex-col">
          <NodeFoldersList
            spaceId={space.id}
            isExpanded={effectiveOpen}
            isCreating={isCreatingFolder}
            onCreatingChange={setIsCreatingFolder}
          />
          <NodeTasksList
            nodeId={space.id}
            parentType={EntityLayerConst.ProjectSpace}
            isExpanded={effectiveOpen}
            spaceId={space.id}
            isCreating={isCreatingTask}
            onCreatingChange={setIsCreatingTask}
          />
        </div>
      </CollapsibleContent>
    </Collapsible>
  );
});
