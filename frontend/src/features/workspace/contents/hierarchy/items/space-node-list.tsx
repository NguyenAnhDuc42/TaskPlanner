import React, { useMemo } from "react";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { useGetNodeSpacesQuery, useSpaces } from "../hierarchy-api";
import { SpaceNodeItem } from "./space-node-item";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";
import { HierarchySidebarSkeleton } from "../hierarchy-components/hierarchy-skeleton";

export const SpaceNodeList = React.memo(function SpaceNodeList({
  searchQuery,
}: {
  searchQuery: string;
}) {
  const { workspaceId } = useWorkspace();
  const { isLoading } = useGetNodeSpacesQuery({ workspaceId: workspaceId || "", cursor: null });
  const spaces = useSpaces();

  const filteredSpaces = useMemo(() => {
    if (!searchQuery) return spaces;
    const query = searchQuery.toLowerCase();
    return spaces.filter((s) => s.name.toLowerCase().includes(query));
  }, [spaces, searchQuery]);

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
          <SpaceNodeItem
            key={s.id}
            spaceId={s.id}
            isForcedOpen={!!searchQuery}
          />
        ))}
      </SortableContext>
    </div>
  );
});
