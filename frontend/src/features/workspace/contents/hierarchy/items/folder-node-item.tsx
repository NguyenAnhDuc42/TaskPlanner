import React, { useState } from "react";
import * as Icons from "lucide-react";
import { ChevronRight, MoreHorizontal, Lock, type LucideIcon } from "lucide-react";
import { useNavigate, useLocation } from "@tanstack/react-router";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { cn } from "@/lib/utils";
import { Collapsible, CollapsibleContent } from "@/components/ui/collapsible";

import { SortableItem } from "../dnd/sortable-item";
import { NodeTasksList } from "./node-tasks-list";
import { FadeTruncate } from "@/components/fade-truncate";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import { useHierarchyStore } from "../use-hierarchy-store";
import { useQueryClient } from "@tanstack/react-query";
import { prefetchNodeTasks } from "../hierarchy-api";
import { FolderContextMenu } from "../hierarchy-components/context-menus/folder-context-menu";
import { EntityMenuTrigger } from "../hierarchy-components/context-menus/shared";

interface FolderNodeItemProps {
  folderId: string;
  spaceId: string;
}

export const FolderNodeItem = React.memo(function FolderNodeItem({
  folderId,
  spaceId,
}: FolderNodeItemProps) {
  const folder = useHierarchyStore((state) => state.folders[folderId]);
  const [isOpen, setIsOpen] = useState(false);
  
  const location = useLocation();
  const navigate = useNavigate();
  const { workspaceId } = useWorkspace();
  const queryClient = useQueryClient();

  // New: Auto-collapse if folder becomes empty
  React.useEffect(() => {
    if (isOpen && folder && !folder.hasTasks) {
      setIsOpen(false);
    }
  }, [isOpen, folder.hasTasks, folder]);

  if (!folder) return null;

  const iconName = (folder.icon ?? "Folder") as keyof typeof Icons;
  const IconComponent: LucideIcon =(Icons[iconName] as unknown as LucideIcon) ?? Icons.Folder;
  
  const isActive = location.pathname.includes(`/folders/${folder.id}`);

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen} className="w-full">
      <SortableItem
        id={`folder-${folder.id}`}
        data={{
          ...folder,
          type: EntityLayerConst.ProjectFolder,
          id: folder.id,
          parentId: spaceId,
          spaceId: spaceId,
        }}
      >
        <FolderContextMenu
          folderId={folder.id}
          folderName={folder.name}
        >
          <div
            className={cn(
              "flex items-center w-full px-1 py-0.5 rounded-sm transition-colors cursor-pointer mb-px group",
              isActive
                ? "bg-primary/10 text-foreground"
                : "text-muted-foreground hover:bg-muted hover:text-foreground",
            )}
            onClick={() =>
              navigate({
                to: `/workspaces/${workspaceId}/folders/${folder.id}`,
              })
            }
          >
            <div
              className="relative flex items-center justify-center w-5 h-5 shrink-0 cursor-pointer rounded-sm hover:bg-background/50 group/icon mr-1.5"
              onMouseEnter={() => {
                if (isOpen || !workspaceId || !folder.hasTasks) return;
                prefetchNodeTasks(
                  queryClient,
                  workspaceId,
                  folder.id,
                  EntityLayerConst.ProjectFolder,
                );
              }}
              onClick={(e) => {
                if (!folder.hasTasks) return;
                e.stopPropagation();
                setIsOpen(!isOpen);
              }}
            >
              <IconComponent
                className={cn(
                  "h-3.5 w-3.5 absolute transition-none",
                  folder.hasTasks && "group-hover/icon:opacity-0",
                )}
                style={{ color: folder.color }}
              />
              {folder.hasTasks && (
                <ChevronRight
                  className={cn(
                    "h-4 w-4 absolute opacity-0 text-muted-foreground group-hover/icon:opacity-100 transition-none",
                    isOpen && "rotate-90",
                  )}
                />
              )}
            </div>
            <FadeTruncate
              text={folder.name}
              className="text-[11px] font-semibold flex-1"
            />

            {/* Action Area: Lock and 3-dots */}
            <div className="flex items-center gap-0.5 min-w-fit">
              {folder.isPrivate && (
                <Lock className="h-3 w-3 text-muted-foreground/40 shrink-0" />
              )}

              <div className="w-0 group-hover:w-4 overflow-hidden opacity-0 group-hover:opacity-100 transition-all duration-300 ease-in-out">
                <EntityMenuTrigger>
                  <button
                    className="h-4 w-4 p-0.5 flex items-center justify-center rounded-sm hover:bg-muted-foreground/10 text-muted-foreground hover:text-primary transition-colors"
                    onClick={(e) => e.stopPropagation()}
                  >
                    <MoreHorizontal className="h-3.5 w-3.5" />
                  </button>
                </EntityMenuTrigger>
              </div>
            </div>
          </div>
        </FolderContextMenu>
      </SortableItem>
      <CollapsibleContent className="overflow-hidden">
        <div className="ml-3.5 pl-2 border-l border-border flex flex-col">
          {isOpen && !folder.hasTasks ? null : (
            <NodeTasksList
              nodeId={folder.id}
              parentType={EntityLayerConst.ProjectFolder}
              isExpanded={isOpen}
              spaceId={spaceId}
            />
          )}
        </div>
      </CollapsibleContent>
    </Collapsible>
  );
});
