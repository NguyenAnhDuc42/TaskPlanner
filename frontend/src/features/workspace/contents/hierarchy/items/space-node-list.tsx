import React, { useMemo } from "react";
import { useDispatch } from "react-redux";
import type { AppDispatch } from "@/store";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useGetNodeSpacesQuery, useSpaces, hierarchyApi } from "../hierarchy-api";
import { SpaceNodeItem } from "./space-node-item";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";
import { HierarchySidebarSkeleton } from "../hierarchy-components/hierarchy-skeleton";

export const SpaceNodeList = React.memo(function SpaceNodeList({
  searchQuery,
}: {
  searchQuery: string;
}) {
  const { workspaceId } = useWorkspace();
  const dispatch = useDispatch<AppDispatch>();

  const { isLoading, isFetching, data } = useGetNodeSpacesQuery({
    workspaceId: workspaceId || "",
    cursor: null,
  });

  const spaces = useSpaces(workspaceId);

  const filteredSpaces = useMemo(() => {
    if (!searchQuery) return spaces;
    const query = searchQuery.toLowerCase();
    return spaces.filter((s) => s.name.toLowerCase().includes(query));
  }, [spaces, searchQuery]);

  const loadMore = () => {
    if (!data?.nextCursor || isFetching) return;
    dispatch(
      hierarchyApi.endpoints.getNodeSpaces.initiate(
        { workspaceId: workspaceId || "", cursor: data.nextCursor },
        { subscribe: false }
      ),
    );
  };

  if (isLoading && spaces.length === 0) {
    return <HierarchySidebarSkeleton />;
  }

  return (
    <div className="flex flex-col">
      <SortableContext
        items={filteredSpaces.map((s) => `space-${s.id}`)}
        strategy={verticalListSortingStrategy}
      >
        {filteredSpaces.map((s) => (
          <SpaceNodeItem key={s.id} spaceId={s.id} isForcedOpen={!!searchQuery} />
        ))}
      </SortableContext>

      {!searchQuery && data?.hasNextPage && (
        <button
          onClick={loadMore}
          disabled={isFetching}
          className="text-[10px] text-muted-foreground/40 hover:text-primary py-1 px-2 text-left transition-colors disabled:opacity-40 font-mono uppercase tracking-tight"
        >
          {isFetching ? "Loading..." : "Load more"}
        </button>
      )}
    </div>
  );
});
