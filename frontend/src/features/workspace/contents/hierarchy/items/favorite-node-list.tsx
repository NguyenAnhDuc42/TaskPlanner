import React, { useMemo, useCallback } from "react";
import { DynamicIcon } from "@/components/dynamic-icon";
import { useNavigate, useLocation, useRouter } from "@tanstack/react-router";
import { useDispatch, useSelector } from "react-redux";
import type { AppDispatch } from "@/store";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useGetFavoritesQuery, useReorderFavoriteMutation, workspaceFeatureApi } from "@/features/workspace/api";
import { spaceSelectors, folderSelectors, taskSelectors } from "@/store/entityStore";
import { EntityLayerType } from "@/types/entity-layer-type";
import { cn } from "@/lib/utils";
import { SpaceContextMenu } from "../hierarchy-components/context-menus/space-context-menu";
import { FolderContextMenu } from "../hierarchy-components/context-menus/folder-context-menu";
import { TaskContextMenu } from "../hierarchy-components/context-menus/task-context-menu";
import { SortableList, SortableListItem } from "@/components/sortable-list";
import { fractionalBetween, fractionalAfter, fractionalBefore } from "@/features/workspace/contents/hierarchy/utils/fractional-index";

type FavItem = {
  id: string;
  name?: string;
  icon?: string;
  color?: string;
  entityLayerType: EntityLayerType;
  favoriteOrderKey?: string;
};

function FavSkeleton() {
  return (
    <div className="flex items-center gap-2 px-1 py-0.5 opacity-20 animate-pulse mb-px">
      <div className="h-3.5 w-3.5 bg-muted-foreground/30 rounded-sm shrink-0" />
      <div className="h-2.5 w-28 bg-muted-foreground/30 rounded-full" />
    </div>
  );
}

function FavItemContent({
  fav,
  isActive: isRouteActive,
  onMouseDown,
  onClick,
  dragHandleProps,
  isDragging,
}: {
  fav: FavItem;
  isActive: boolean;
  onMouseDown: () => void;
  onClick: () => void;
  dragHandleProps: Record<string, unknown>;
  isDragging: boolean;
}) {
  const isTask = fav.entityLayerType === EntityLayerType.ProjectTask;
  const iconName = fav.icon ?? (isTask ? "CheckSquare" : fav.entityLayerType === EntityLayerType.ProjectFolder ? "Folder" : "LayoutGrid");

  const button = (
    <button
      type="button"
      onMouseDown={onMouseDown}
      onClick={onClick}
      className={cn(
        "flex items-center px-1 py-0.5 rounded-md transition-colors mb-px border w-full text-left outline-none select-none cursor-grab active:cursor-grabbing",
        isDragging ? "opacity-50" : "",
        isRouteActive
          ? "bg-primary/10 text-primary border-primary/25"
          : "text-muted-foreground border-transparent hover:bg-muted/50 hover:text-foreground hover:border-border/30",
      )}
      {...dragHandleProps}
    >
      <div className="w-5 h-5 flex items-center justify-center shrink-0 mr-1.5">
        <DynamicIcon name={iconName} size={14} color={fav.color || undefined} className="transition-none" />
      </div>
      <span className="text-[11px] font-semibold leading-tight truncate whitespace-nowrap">
        {fav.name ?? "—"}
      </span>
    </button>
  );

  if (fav.entityLayerType === EntityLayerType.ProjectSpace)
    return <SpaceContextMenu key={fav.id} spaceId={fav.id} spaceName={fav.name ?? ""}>{button}</SpaceContextMenu>;
  if (fav.entityLayerType === EntityLayerType.ProjectFolder)
    return <FolderContextMenu key={fav.id} folderId={fav.id} folderName={fav.name ?? ""}>{button}</FolderContextMenu>;
  return <TaskContextMenu key={fav.id} taskId={fav.id} taskName={fav.name ?? ""} parentId="">{button}</TaskContextMenu>;
}

export const FavoriteNodeList = React.memo(function FavoriteNodeList() {
  const { workspaceId } = useWorkspace();
  const dispatch = useDispatch<AppDispatch>();
  const navigate = useNavigate();
  const router = useRouter();
  const location = useLocation();
  const [reorderFavorite] = useReorderFavoriteMutation();

  const { isLoading, isFetching, data } = useGetFavoritesQuery(
    { workspaceId: workspaceId || "", cursor: null },
    { skip: !workspaceId }
  );

  const allSpaces  = useSelector(spaceSelectors.selectAll);
  const allFolders = useSelector(folderSelectors.selectAll);
  const allTasks   = useSelector(taskSelectors.selectAll);

  const favorites = useMemo<FavItem[]>(() => {
    const items: FavItem[] = [
      ...allSpaces.filter(s => s.isFavorite && s.workspaceId === workspaceId).map(s => ({
        id: s.id, name: s.name, icon: s.icon, color: s.color,
        entityLayerType: EntityLayerType.ProjectSpace,
        favoriteOrderKey: s.favoriteOrderKey,
      })),
      ...allFolders.filter(f => f.isFavorite && f.workspaceId === workspaceId).map(f => ({
        id: f.id, name: f.name, icon: f.icon, color: f.color,
        entityLayerType: EntityLayerType.ProjectFolder,
        favoriteOrderKey: f.favoriteOrderKey,
      })),
      ...allTasks.filter(t => t.isFavorite && t.workspaceId === workspaceId).map(t => ({
        id: t.id, name: t.name, icon: t.icon, color: t.color,
        entityLayerType: EntityLayerType.ProjectTask,
        favoriteOrderKey: t.favoriteOrderKey,
      })),
    ];
    return items.sort((a, b) =>
      ((a.favoriteOrderKey ?? "") < (b.favoriteOrderKey ?? "") ? -1 : 1)
    );
  }, [allSpaces, allFolders, allTasks, workspaceId]);

  const handleReorder = useCallback((reordered: FavItem[]) => {
    if (!workspaceId) return;
    const draggedIdx = reordered.findIndex((f, i) => favorites[i]?.id !== f.id);
    const moved = reordered[draggedIdx];
    if (!moved) return;
    const prev = reordered[draggedIdx - 1]?.favoriteOrderKey ?? null;
    const next = reordered[draggedIdx + 1]?.favoriteOrderKey ?? null;
    const newOrderKey = prev && next
      ? fractionalBetween(prev, next)
      : prev ? fractionalAfter(prev)
      : next ? fractionalBefore(next)
      : moved.favoriteOrderKey ?? "";
    reorderFavorite({ workspaceId, entityId: moved.id, entityType: moved.entityLayerType, previousOrderKey: prev, nextOrderKey: next, newOrderKey });
  }, [favorites, workspaceId, reorderFavorite]);

  const getIsActive = (id: string, type: EntityLayerType) => {
    if (type === EntityLayerType.ProjectSpace)  return location.pathname.includes(`/spaces/${id}`);
    if (type === EntityLayerType.ProjectFolder) return location.pathname.includes(`/folders/${id}`);
    if (type === EntityLayerType.ProjectTask)   return location.pathname.includes(`/tasks/${id}`);
    return false;
  };

  const handleMouseDown = (id: string, type: EntityLayerType) => {
    if (!workspaceId) return;
    if (type === EntityLayerType.ProjectSpace)
      router.preloadRoute({ to: "/workspaces/$workspaceId/spaces/$spaceId", params: { workspaceId, spaceId: id } });
    else if (type === EntityLayerType.ProjectFolder)
      router.preloadRoute({ to: "/workspaces/$workspaceId/folders/$folderId", params: { workspaceId, folderId: id } });
    else if (type === EntityLayerType.ProjectTask)
      router.preloadRoute({ to: "/workspaces/$workspaceId/tasks/$taskId", params: { workspaceId, taskId: id } });
  };

  const handleClick = (id: string, type: EntityLayerType) => {
    if (!workspaceId) return;
    if (type === EntityLayerType.ProjectSpace)
      navigate({ to: "/workspaces/$workspaceId/spaces/$spaceId", params: { workspaceId, spaceId: id } });
    else if (type === EntityLayerType.ProjectFolder)
      navigate({ to: "/workspaces/$workspaceId/folders/$folderId", params: { workspaceId, folderId: id } });
    else if (type === EntityLayerType.ProjectTask)
      navigate({ to: "/workspaces/$workspaceId/tasks/$taskId", params: { workspaceId, taskId: id } });
  };

  const loadMore = () => {
    if (!data?.nextCursor || isFetching) return;
    dispatch(workspaceFeatureApi.endpoints.getFavorites.initiate(
      { workspaceId: workspaceId || "", cursor: data.nextCursor },
      { subscribe: false }
    ));
  };

  if (isLoading && favorites.length === 0) {
    return <div className="flex flex-col">{[1, 2, 3].map(i => <FavSkeleton key={i} />)}</div>;
  }

  if (!isLoading && favorites.length === 0) {
    return <p className="text-[10px] text-muted-foreground/30 px-1 py-0.5 italic">No favorites yet</p>;
  }

  return (
    <>
      <SortableList items={favorites} onReorder={handleReorder} direction="vertical" className="flex flex-col" activationDistance={4}>
        {favorites.map(fav => (
          <SortableListItem key={fav.id} id={fav.id}>
            {({ dragHandleProps, isDragging }) => (
              <FavItemContent
                fav={fav}
                isActive={getIsActive(fav.id, fav.entityLayerType)}
                onMouseDown={() => handleMouseDown(fav.id, fav.entityLayerType)}
                onClick={() => handleClick(fav.id, fav.entityLayerType)}
                dragHandleProps={dragHandleProps}
                isDragging={isDragging}
              />
            )}
          </SortableListItem>
        ))}
      </SortableList>

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
