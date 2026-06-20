import { useGetNodeFoldersQuery, useFoldersBySpace, hierarchyApi } from "@/features/workspace/contents/hierarchy/hierarchy-api";
import { FolderNodeItem } from "@/features/workspace/contents/hierarchy/items/folder-node-item";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable";
import React from "react";
import { useDispatch } from "react-redux";
import type { AppDispatch } from "@/store";
import { spaceSlice } from "@/store/entityStore";

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
  const dispatch = useDispatch<AppDispatch>();

  const { isLoading, isFetching, data } = useGetNodeFoldersQuery(
    { workspaceId: workspaceId || "", nodeId: spaceId, cursor: null },
    { skip: !isExpanded },
  );

  const folders = useFoldersBySpace(spaceId);

  React.useEffect(() => {
    if (isExpanded && !isLoading && folders.length === 0) {
      dispatch(spaceSlice.actions.upsert({ id: spaceId, hasFolders: false }));
    }
  }, [isExpanded, isLoading, folders.length, spaceId, dispatch]);

  if (!isExpanded) return null;

  if (isLoading && folders.length === 0) {
    return (
      <div className="flex flex-col gap-0.5">
        {[1, 2].map((i) => (
          <FolderSkeleton key={i} />
        ))}
      </div>
    );
  }

  const loadMore = () => {
    if (!data?.nextCursor || isFetching) return;
    dispatch(
      hierarchyApi.endpoints.getNodeFolders.initiate(
        { workspaceId: workspaceId || "", nodeId: spaceId, cursor: data.nextCursor },
        { subscribe: false }
      ),
    );
  };

  return (
    <>
      <SortableContext
        items={folders.map((f) => `folder-${f.id}`)}
        strategy={verticalListSortingStrategy}
      >
        <div className="flex flex-col">
          {folders.map((f) => (
            <FolderNodeItem key={f.id} folderId={f.id} spaceId={spaceId} />
          ))}
        </div>
      </SortableContext>

      {data?.hasNextPage && (
        <button
          onClick={loadMore}
          disabled={isFetching}
          className="text-[10px] text-muted-foreground/40 hover:text-primary py-0.5 px-1 text-left transition-colors disabled:opacity-40 font-mono uppercase tracking-tight"
        >
          {isFetching ? "Loading..." : "Load more"}
        </button>
      )}
    </>
  );
});
