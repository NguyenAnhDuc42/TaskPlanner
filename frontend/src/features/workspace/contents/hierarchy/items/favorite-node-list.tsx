import React, { useMemo, useCallback } from "react";
import * as Icons from "lucide-react";
import { CheckSquare } from "lucide-react";
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
import {
  DndContext,
  PointerSensor,
  closestCenter,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  verticalListSortingStrategy,
  useSortable,
  arrayMove,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { restrictToVerticalAxis } from "@dnd-kit/modifiers";
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

function SortableFavItem({
  fav,
  isActive: isRouteActive,
  onMouseDown,
  onClick,
}: {
  fav: FavItem;
  isActive: boolean;
  onMouseDown: () => void;
  onClick: () => void;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: fav.id });

  const isTask = fav.entityLayerType === EntityLayerType.ProjectTask;
  const iconName = (fav.icon ?? (isTask ? "CheckSquare" : fav.entityLayerType === EntityLayerType.ProjectFolder ? "Folder" : "LayoutGrid")) as keyof typeof Icons;
  const Icon = (Icons[iconName] as Icons.LucideIcon) ?? (isTask ? CheckSquare : Icons.LayoutGrid);

  const button = (
    <button
      ref={setNodeRef}
      type="button"
      style={{ transform: CSS.Transform.toString(transform), transition }}
      onMouseDown={onMouseDown}
      onClick={onClick}
      className={cn(
        "flex items-center px-1 py-0.5 rounded-sm transition-colors mb-px border w-full text-left outline-none select-none cursor-grab active:cursor-grabbing",
        isDragging ? "opacity-50" : "",
        isRouteActive
          ? "bg-primary/10 text-primary border-primary/25"
          : "text-muted-foreground border-transparent hover:bg-muted/50 hover:text-foreground hover:border-border/30",
      )}
      {...attributes}
      {...listeners}
    >
      <div className="w-5 h-5 flex items-center justify-center shrink-0 mr-1.5">
        <Icon className="h-3.5 w-3.5 transition-none" style={{ color: fav.color || undefined }} />
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

  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 4 } }));

  const handleDragEnd = useCallback((event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id || !workspaceId) return;

    const oldIndex = favorites.findIndex(f => f.id === active.id);
    const newIndex = favorites.findIndex(f => f.id === over.id);
    if (oldIndex === -1 || newIndex === -1) return;

    const reordered = arrayMove(favorites, oldIndex, newIndex);
    const moved = reordered[newIndex];
    const prev = reordered[newIndex - 1]?.favoriteOrderKey ?? null;
    const next = reordered[newIndex + 1]?.favoriteOrderKey ?? null;

    const newOrderKey = prev && next
      ? fractionalBetween(prev, next)
      : prev
        ? fractionalAfter(prev)
        : next
          ? fractionalBefore(next)
          : moved.favoriteOrderKey ?? "";

    reorderFavorite({
      workspaceId,
      entityId: moved.id,
      entityType: moved.entityLayerType,
      previousOrderKey: prev,
      nextOrderKey: next,
      newOrderKey,
    });
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
      <DndContext
        sensors={sensors}
        collisionDetection={closestCenter}
        modifiers={[restrictToVerticalAxis]}
        onDragEnd={handleDragEnd}
      >
        <SortableContext items={favorites.map(f => f.id)} strategy={verticalListSortingStrategy}>
          <div className="flex flex-col">
            {favorites.map(fav => (
              <SortableFavItem
                key={fav.id}
                fav={fav}
                isActive={getIsActive(fav.id, fav.entityLayerType)}
                onMouseDown={() => handleMouseDown(fav.id, fav.entityLayerType)}
                onClick={() => handleClick(fav.id, fav.entityLayerType)}
              />
            ))}
          </div>
        </SortableContext>
      </DndContext>

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
