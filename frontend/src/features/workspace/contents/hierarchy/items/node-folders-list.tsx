import { useNodeFolders } from "@/features/workspace/contents/hierarchy/hierarchy-api";

import { FolderNodeItem } from "@/features/workspace/contents/hierarchy/items/folder-node-item";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";
import React from "react";
import type { FolderHierarchy } from "../hierarchy-type";

const FolderSkeleton = () => (
  <div className="flex items-center gap-2 pl-4 py-1 opacity-20 animate-pulse">
    <div className="h-3.5 w-4 bg-muted-foreground/30 rounded-sm" />
    <div className="h-2.5 w-24 bg-muted-foreground/30 rounded-full" />
  </div>
);

export const NodeFoldersList = React.memo(function NodeFoldersList({
  spaceId,
  isExpanded,
}: {
  spaceId: string;
  isExpanded: boolean;
}) {
  const { workspaceId } = useWorkspace();
  
  // Only load folders if this space is expanded
  const { data: folders = [], isLoading } = useNodeFolders(
    workspaceId || "", 
    spaceId, 
    isExpanded
  );

  if (!isExpanded) return null;

  if (isLoading) {
    return (
      <div className="flex flex-col gap-0.5">
        {[1, 2].map((i) => (
          <FolderSkeleton key={i} />
        ))}
      </div>
    );
  }

  if (folders.length === 0) return null;

  return (
    <SortableContext
      items={folders.map((f) => `folder-${f.id}`)}
      strategy={verticalListSortingStrategy}
    >
      <div className="flex flex-col">
        {folders.map((folder: FolderHierarchy) => (
          <FolderNodeItem
            key={folder.id}
            folder={folder}
            spaceId={spaceId}
          />
        ))}
      </div>
    </SortableContext>
  );
});
