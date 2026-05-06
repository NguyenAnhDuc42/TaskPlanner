import React, { useState } from "react";
import * as Icons from "lucide-react";
import { ChevronRight, MoreHorizontal } from "lucide-react";
import { useNavigate, useLocation } from "@tanstack/react-router";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { cn } from "@/lib/utils";
import { Collapsible, CollapsibleContent } from "@/components/ui/collapsible";
import { DropdownWrapper } from "@/components/dropdown-wrapper";
import { FolderMenu } from "../hierarchy-components/dropdown/folder-menu";
import { SortableItem } from "../dnd/sortable-item";
import { NodeTasksList } from "./node-tasks-list";
import { clampName } from "../utils/name-utils";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import type { FolderHierarchy } from "../hierarchy-type";
import { useQueryClient } from "@tanstack/react-query";
import { prefetchNodeTasks } from "../hierarchy-api";

interface FolderNodeItemProps {
  folder: FolderHierarchy;
  spaceId: string;
}

export const FolderNodeItem = React.memo(function FolderNodeItem({
  folder,
  spaceId,
}: FolderNodeItemProps) {
  const [isOpen, setIsOpen] = useState(false);
  const IconComponent = (Icons as any)[folder.icon] || Icons.Folder;
  const location = useLocation();
  const isActive = location.pathname.includes(`/folders/${folder.id}`);
  const navigate = useNavigate();
  const { workspaceId } = useWorkspace();
  const queryClient = useQueryClient();

  // New: Auto-collapse if folder becomes empty
  React.useEffect(() => {
    if (isOpen && !folder.hasTasks) {
      setIsOpen(false);
    }
  }, [isOpen, folder.hasTasks]);

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
        <div
          className={cn(
            "flex items-center w-full px-1 py-0.5 rounded-sm transition-colors cursor-pointer mb-px group",
            isActive
              ? "bg-primary/10 text-foreground"
              : "text-muted-foreground hover:bg-muted hover:text-foreground",
          )}
          onClick={() =>
            navigate({ to: `/workspaces/${workspaceId}/folders/${folder.id}` })
          }
        >
          <div
            className="relative flex items-center justify-center w-5 h-5 flex-shrink-0 cursor-pointer rounded-sm hover:bg-background/50 group/icon mr-1.5"
            onMouseEnter={() => {
              if (isOpen || !workspaceId || !folder.hasTasks) return;

              // Eager prefetch: Start immediately on hover
              prefetchNodeTasks(
                queryClient,
                workspaceId,
                folder.id,
                EntityLayerConst.ProjectFolder,
              );
            }}
            id={`folder-expand-${folder.id}`}
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
          <span className="truncate text-[11px] font-semibold flex-1">
            {clampName(folder.name)}
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
              <FolderMenu folderId={folder.id} spaceId={spaceId} />
            </DropdownWrapper>
          </div>
        </div>
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
