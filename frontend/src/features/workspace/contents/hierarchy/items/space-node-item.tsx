import React from "react";
import * as Icons from "lucide-react";
import { ChevronRight, MoreVertical, Lock, type LucideIcon } from "lucide-react";
import { useNavigate, useLocation, useRouter } from "@tanstack/react-router";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { cn } from "@/lib/utils";
import { Collapsible, CollapsibleContent } from "@/components/ui/collapsible";
import { useSelector } from "react-redux";
import { spaceSelectors } from "@/store/entityStore";
import { hierarchyApi } from "../hierarchy-api";
import type { RootState } from "@/store";
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

export const SpaceNodeItem = React.memo(function SpaceNodeItem({
  spaceId,
  isForcedOpen = false,
}: SpaceNodeItemProps) {
  const { workspaceId } = useWorkspace();
  
  const space = useSelector((state: RootState) => spaceSelectors.selectById(state, spaceId));
  const hasChildren = !!(space?.hasFolders || space?.hasTasks);
  
  const prefetchFolders = hierarchyApi.usePrefetch("getNodeFolders");
  const prefetchTasks = hierarchyApi.usePrefetch("getNodeTasks");

  const navigate = useNavigate();
  const router = useRouter();
  const location = useLocation();
  const [isOpen, setIsOpen] = React.useState(false);


  if (!space) return null;

  const isActive = location.pathname.includes(`/spaces/${space.id}`);
  const iconName = (space.icon ?? "LayoutGrid") as keyof typeof Icons;
  const IconComponent: LucideIcon =(Icons[iconName] as unknown as LucideIcon) ?? Icons.LayoutGrid;
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
        <SpaceContextMenu spaceId={space.id} spaceName={space.name}>
          <div
            className={cn(
              "flex items-center w-full px-1 py-0.5 rounded-sm transition-colors mb-px group",
              isActive
                ? "bg-primary/10 text-primary"
                : "text-foreground hover:bg-muted",
            )}
          >
            <button
              type="button"
              className="relative flex items-center justify-center w-5 h-5 shrink-0 cursor-pointer rounded-sm hover:bg-background/50 group/icon mr-1.5"
              onMouseEnter={() => {
                if (effectiveOpen || !workspaceId) return;
                if (space.hasFolders) prefetchFolders({ workspaceId, nodeId: space.id, cursor: null });
                if (space.hasTasks) prefetchTasks({ workspaceId, nodeId: space.id, parentType: EntityLayerConst.ProjectSpace, cursor: null });
              }}
              onClick={(e) => {
                if (!hasChildren) return;
                e.stopPropagation();
                setIsOpen(!isOpen);
              }}
            >
              <IconComponent
                className={cn(
                  "h-3.5 w-3.5 absolute transition-none",
                  hasChildren && "group-hover/icon:opacity-0",
                )}
                style={{ color: spaceColor }}
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
              className="flex-1 text-left text-[11px] font-bold truncate outline-none select-none"
              onMouseDown={() => {
                if (workspaceId) {
                  router.preloadRoute({
                    to: "/workspaces/$workspaceId/spaces/$spaceId",
                    params: { workspaceId, spaceId: space.id },
                  });
                }
              }}
              onClick={() => navigate({
                  to: "/workspaces/$workspaceId/spaces/$spaceId",
                  params: { workspaceId, spaceId: space.id },
                })}
            >
              {space.name}
            </button>

            <div className="flex items-center gap-0.5 min-w-fit">
              {space.isPrivate && (
                <Lock className="h-3 w-3 text-muted-foreground/40 shrink-0 mr-1" />
              )}

              <div className="w-0 group-hover:w-4 overflow-hidden opacity-0 group-hover:opacity-100 transition-all duration-300 ease-in-out">
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
          </div>
        </SpaceContextMenu>
      </SortableItem>

      <CollapsibleContent className="overflow-hidden">
        <div className="ml-3.25 pl-2 border-l border-border flex flex-col">
          <NodeFoldersList
            spaceId={space.id}
            isExpanded={effectiveOpen}
          />
          <NodeTasksList
            nodeId={space.id}
            parentType={EntityLayerConst.ProjectSpace}
            isExpanded={effectiveOpen}
            spaceId={space.id}
          />
        </div>
      </CollapsibleContent>
    </Collapsible>
  );
});
