import React from "react";
import * as Icons from "lucide-react";
import {
  ChevronRight,
  MoreHorizontal,
  Lock,
} from "lucide-react";
import { useNavigate, useLocation } from "@tanstack/react-router";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { cn } from "@/lib/utils";
import { Collapsible, CollapsibleContent } from "@/components/ui/collapsible";
import { DropdownWrapper } from "@/components/dropdown-wrapper";

import { SpaceMenu } from "../hierarchy-components/dropdown/space-menu";
import { FolderItem } from "./folder-item";
import { NodeTasksList } from "./node-tasks-list";
import { clampName } from "../utils/name-utils";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import {
  SortableContext,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import {
  prefetchNodeFolders,
  prefetchNodeTasks,
  useNodeFolders,
} from "../hierarchy-api";
import type { SpaceHierarchy } from "../hierarchy-type";
import { useQueryClient } from "@tanstack/react-query";
import { SortableItem } from "../dnd/sortable-item";

interface SpaceItemProps {
  space: SpaceHierarchy;
  isForcedOpen?: boolean;
}

export const SpaceItem = React.memo(function SpaceItem({
  space,
  isForcedOpen,
}: SpaceItemProps) {
  const { workspaceId } = useWorkspace();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const location = useLocation();
  const isActive = location.pathname.includes(`/spaces/${space.id}`);
  const IconComponent = (Icons as any)[space.icon] || Icons.LayoutGrid;
  const spaceColor = space.color || "var(--primary)";

  // Extract folderId from URL if present (e.g. /folders/abc-123)
  const activeFolderIdFromUrl = React.useMemo(() => {
    const match = location.pathname.match(/\/folders\/([^/]+)/);
    return match ? match[1] : null;
  }, [location.pathname]);

  const [isOpen, setIsOpen] = React.useState(false);
  const effectiveOpen = isForcedOpen || isOpen;

  const hasChildren = space.hasFolders || space.hasTasks;

  // Eagerly load folders if a folderId is in the URL (to check ownership)
  const shouldLoadFolders = effectiveOpen || (!!activeFolderIdFromUrl && space.hasFolders);

  const { data: spaceFolders = [], isLoading: isLoadingFolders } =
    useNodeFolders(workspaceId || "", space.id, shouldLoadFolders);

  // Auto-expand this space if it owns the folder in the URL
  React.useEffect(() => {
    if (activeFolderIdFromUrl && spaceFolders.length > 0) {
      const ownsFolder = spaceFolders.some(f => f.id === activeFolderIdFromUrl);
      if (ownsFolder && !isOpen) {
        setIsOpen(true);
      }
    }
  }, [activeFolderIdFromUrl, spaceFolders]);

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
        <div
          className={cn(
            "flex items-center w-full px-1 py-0.5 rounded-sm transition-colors cursor-pointer mb-px group",
            isActive
              ? "bg-primary/10 text-primary"
              : "text-foreground hover:bg-muted",
          )}
          onClick={() =>
            navigate({ to: `/workspaces/${workspaceId}/spaces/${space.id}` })
          }
        >
          <div
            className="relative flex items-center justify-center w-5 h-5 flex-shrink-0 cursor-pointer rounded-sm hover:bg-background/50 group/icon mr-0.5"
            onMouseEnter={() => {
              if (effectiveOpen || !workspaceId || !hasChildren) return;

              // Eager prefetch: Start immediately on hover for maximum speed
              prefetchNodeFolders(queryClient, workspaceId, space.id);
              prefetchNodeTasks(
                queryClient,
                workspaceId,
                space.id,
                EntityLayerConst.ProjectSpace,
              );
            }}
            id={`space-expand-${space.id}`}
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
              style={{ color: isActive ? spaceColor : undefined }}
            />
            {hasChildren && (
              <ChevronRight
                className={cn(
                  "h-4 w-4 absolute opacity-0 text-muted-foreground group-hover/icon:opacity-100 transition-none",
                  isOpen && "rotate-90",
                )}
              />
            )}
          </div>
          <span className="truncate text-[11px] font-bold flex-1">
            {clampName(space.name)}
          </span>
          <div className="opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-1">
            <DropdownWrapper
              align="start"
              side="right"
              trigger={
                <button
                  className="h-4 w-4 p-0.5 flex items-center justify-center rounded-sm hover:bg-muted-foreground/10 text-muted-foreground hover:text-primary transition-colors"
                  onClick={(e) => e.stopPropagation()}
                >
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

      <CollapsibleContent className="overflow-hidden">
        <div className="ml-3 pl-2 border-l border-border flex flex-col">
          {isLoadingFolders ? (
            <div className="flex flex-col gap-1 py-1">
              <div className="h-5 w-32 bg-muted/40 animate-pulse rounded-sm ml-2" />
              <div className="h-5 w-24 bg-muted/40 animate-pulse rounded-sm ml-2" />
            </div>
          ) : (
            <>
              {spaceFolders.length > 0 && (
                <SortableContext
                  items={spaceFolders.map((f) => `folder-${f.id}`)}
                  strategy={verticalListSortingStrategy}
                >
                  {spaceFolders.map((folder) => (
                    <FolderItem
                      key={folder.id}
                      folder={folder}
                      spaceId={space.id}
                    />
                  ))}
                </SortableContext>
              )}
            </>
          )}
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
