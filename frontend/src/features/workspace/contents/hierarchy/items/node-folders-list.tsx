import { useNodeFolders } from "@/features/workspace/contents/hierarchy/hierarchy-api";

import { FolderNodeItem } from "@/features/workspace/contents/hierarchy/items/folder-node-item";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";
import React from "react";
import { useHierarchyStore } from "../use-hierarchy-store";

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
  
  const { data, isLoading, fetchNextPage, hasNextPage } = useNodeFolders(
    workspaceId || "", 
    spaceId, 
    isExpanded
  );
  
  const setFolders = useHierarchyStore((state) => state.setFolders);
  const folderIds = useHierarchyStore((state) => state.foldersBySpace[spaceId] || []);

  React.useEffect(() => {
    if (data?.pages) {
      const allFolders = data.pages.flatMap((page) => page.items);
      setFolders(spaceId, allFolders);
    }
  }, [data, setFolders, spaceId]);

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

  if (folderIds.length === 0) return null;

  return (
    <SortableContext
      items={folderIds.map((id) => `folder-${id}`)}
      strategy={verticalListSortingStrategy}
    >
      <div className="flex flex-col">
        {folderIds.map((id) => (
          <FolderNodeItem
            key={id}
            folderId={id}
            spaceId={spaceId}
          />
        ))}
      </div>
      {hasNextPage && (
        <button
          onClick={(e) => {
             e.stopPropagation();
             fetchNextPage();
          }}
          className="text-[10px] text-muted-foreground hover:text-primary mt-1 text-left px-2"
        >
          Load more folders...
        </button>
      )}
    </SortableContext>
  );
});
