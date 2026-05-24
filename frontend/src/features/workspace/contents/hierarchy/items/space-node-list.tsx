import React, { useEffect, useMemo } from "react";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { useHierarchyStore } from "../use-hierarchy-store";
import { useNodeSpaces } from "../hierarchy-api";
import { SpaceNodeItem } from "./space-node-item";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";
import { HierarchySidebarSkeleton } from "../hierarchy-components/hierarchy-skeleton";

export const SpaceNodeList = React.memo(function SpaceNodeList({
  searchQuery,
}: {
  searchQuery: string;
}) {
  const { workspaceId } = useWorkspace();
  const { data, isLoading, fetchNextPage, hasNextPage } = useNodeSpaces(workspaceId || "");
  const setSpaces = useHierarchyStore((state) => state.setSpaces);
  const rootSpaceIds = useHierarchyStore((state) => state.rootSpaceIds);
  const spaces = useHierarchyStore((state) => state.spaces);

  useEffect(() => {
    if (data?.pages) {
      const allSpaces = data.pages.flatMap((page) => page.items);
      setSpaces(allSpaces);
    }
  }, [data, setSpaces]);

  const filteredSpaceIds = useMemo(() => {
    if (!searchQuery) return rootSpaceIds;
    const query = searchQuery.toLowerCase();
    return rootSpaceIds.filter((id) => {
      const space = spaces[id];
      if (!space) return false;
      return space.name.toLowerCase().includes(query);
    });
  }, [rootSpaceIds, spaces, searchQuery]);

  if (isLoading && rootSpaceIds.length === 0) {
    return <HierarchySidebarSkeleton />;
  }

  return (
    <div className="flex flex-col">
      <SortableContext
        items={filteredSpaceIds.map((id) => `space-${id}`)}
        strategy={verticalListSortingStrategy}
      >
        {filteredSpaceIds.map((id) => (
          <SpaceNodeItem
            key={id}
            spaceId={id}
            isForcedOpen={!!searchQuery}
          />
        ))}
      </SortableContext>
      {hasNextPage && (
        <button
          onClick={() => fetchNextPage()}
          className="text-[10px] text-muted-foreground hover:text-primary mt-1 text-left px-2"
        >
          Load more...
        </button>
      )}
    </div>
  );
});
