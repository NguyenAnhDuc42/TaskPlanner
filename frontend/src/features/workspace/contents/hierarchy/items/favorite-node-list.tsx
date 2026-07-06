import { useMemo, useCallback } from "react";
import { observer } from "mobx-react-lite";
import { DynamicIcon } from "@/components/dynamic-icon";
import { useNavigate, useLocation, useRouter } from "@tanstack/react-router";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { FavoriteMutations } from "@/mutations/favorite.mutations";
import { EntityLayerType } from "@/types/entity-layer-type";
import { cn } from "@/lib/utils";
import { SpaceContextMenu } from "../hierarchy-components/context-menus/space-context-menu";
import { FolderContextMenu } from "../hierarchy-components/context-menus/folder-context-menu";
import { TaskContextMenu } from "../hierarchy-components/context-menus/task-context-menu";
import { SortableList, SortableListItem } from "@/components/sortable-list";
import { fractionalBetween, fractionalAfter, fractionalBefore, fractionalStart } from "@/features/workspace/contents/hierarchy/utils/fractional-index";

type FavItem = {
  id: string; // entityId — used for navigation, context menus, and drag identity
  name?: string;
  icon?: string;
  color?: string;
  entityLayerType: EntityLayerType;
  favoriteOrderKey?: string;
};

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
  dragHandleProps?: Record<string, unknown>;
  isDragging?: boolean;
}) {
  const isTask = fav.entityLayerType === EntityLayerType.ProjectTask;
  const iconName = fav.icon ?? (isTask ? "CheckSquare" : fav.entityLayerType === EntityLayerType.ProjectFolder ? "Folder" : "LayoutGrid");

  const button = (
    <button
      type="button"
      onMouseDown={onMouseDown}
      onClick={onClick}
      className={cn(
        "flex items-center px-1 py-0.5 rounded-md transition-colors mb-px border w-full text-left outline-none select-none cursor-pointer active:cursor-grabbing",
        isDragging ? "opacity-50" : "",
        isRouteActive
          ? "bg-primary/5 text-primary border-primary/25"
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

export const FavoriteNodeList = observer(function FavoriteNodeList() {
  const { workspaceId } = useWorkspace();
  const rootStore = useWorkspaceRootStore();
  const favoriteMutations = useMemo(() => new FavoriteMutations(rootStore), [rootStore]);
  const navigate = useNavigate();
  const router = useRouter();
  const location = useLocation();

  // Plain read, not useMemo — this is a mobx-react-lite observer, which tracks observable reads
  // made directly during render. `.all` returns a fresh array on every call, so a useMemo keyed on
  // it never actually caches anything (deps always look changed) — it only guarantees a brand-new
  // `favorites` array reference every render, which confuses dnd-kit's drag position tracking.
  //
  // Favorite membership/order comes from favoriteStore alone — name/icon/color are a display-only
  // lookup into the entity's own store. A favorite pointing at an entity that's since been deleted
  // (or not yet hydrated) is silently skipped rather than rendered with blank/broken content.
  const lookupEntity = (entityId: string, type: EntityLayerType) => {
    if (type === EntityLayerType.ProjectSpace) return rootStore.spaceStore.getById(entityId);
    if (type === EntityLayerType.ProjectFolder) return rootStore.folderStore.getById(entityId);
    return rootStore.taskStore.getById(entityId);
  };

  const favorites: FavItem[] = rootStore.favoriteStore.all
    .map((f): FavItem | null => {
      const entity = lookupEntity(f.entityId, f.entityLayerType);
      if (!entity) return null;
      return {
        id: f.entityId,
        name: entity.name,
        icon: entity.icon,
        color: entity.color,
        entityLayerType: f.entityLayerType,
        favoriteOrderKey: f.orderKey,
      };
    })
    .filter((f): f is FavItem => f !== null)
    .sort((a, b) => ((a.favoriteOrderKey ?? "") < (b.favoriteOrderKey ?? "") ? -1 : 1));

  // Robust to any state the neighbors' keys are in — missing, equal, out of order, whatever.
  // fractionalBetween/After/Before already tolerate garbage input via safeKey() (falls back to
  // null rather than throwing), and only ever produce ONE new key for the MOVED item — the other
  // items' keys are never touched. The one case that used to slip through: no prev AND no next
  // (e.g. the whole list had no real keys yet), which fell back to an empty string — the backend
  // rejects an empty OrderKey, so that reorder silently failed. fractionalStart() replaces that.
  //
  // draggedId comes straight from dnd-kit's active.id, not from diffing old vs new order — diffing
  // ("first index that differs") breaks for a front-to-back move: shifting the first item to last
  // shifts every index, so the first differing index is always 0 — the item that slid INTO that
  // slot, not the one actually dragged. That bug meant dragging the first favorite to the bottom
  // silently reordered a DIFFERENT item instead, leaving the dragged one stuck in place forever.
  const handleReorder = useCallback((reordered: FavItem[], draggedId: string) => {
    if (!workspaceId) return;
    const draggedIdx = reordered.findIndex((f) => f.id === draggedId);
    const moved = reordered[draggedIdx];
    if (!moved) return;
    const prev = reordered[draggedIdx - 1]?.favoriteOrderKey ?? null;
    const next = reordered[draggedIdx + 1]?.favoriteOrderKey ?? null;
    const newOrderKey = prev && next
      ? fractionalBetween(prev, next)
      : prev ? fractionalAfter(prev)
      : next ? fractionalBefore(next)
      : fractionalStart();
    favoriteMutations.reorder(moved.id, moved.entityLayerType, prev, next, newOrderKey)
      .catch((err) => console.error("Failed to reorder favorite", err));
  }, [workspaceId, favoriteMutations]);

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

  if (favorites.length === 0) {
    return <p className="text-[10px] text-muted-foreground/30 px-1 py-0.5 italic">No favorites yet</p>;
  }

  return (
    <SortableList
      items={favorites}
      onReorder={handleReorder}
      direction="vertical"
      className="flex flex-col"
      activationDistance={4}
      renderOverlay={(draggedId) => {
        const fav = favorites.find(f => f.id === draggedId);
        if (!fav) return null;
        return (
          <div className="w-52 rounded-md bg-popover border border-border/50 shadow-xl">
            <FavItemContent
              fav={fav}
              isActive={false}
              onMouseDown={() => {}}
              onClick={() => {}}
            />
          </div>
        );
      }}
    >
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
  );
});
