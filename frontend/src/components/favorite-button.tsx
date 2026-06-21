import React from "react";
import { Star } from "lucide-react";
import { cn } from "@/lib/utils";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useToggleFavoriteMutation } from "@/features/workspace/api";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { useSelector } from "react-redux";
import { spaceSelectors, folderSelectors, taskSelectors } from "@/store/entityStore";
import type { RootState } from "@/store";

interface FavoriteButtonProps {
  entityId: string;
  entityLayerType: EntityLayerType;
  className?: string;
  iconSize?: number;
}

export const FavoriteButton = ({
  entityId,
  entityLayerType,
  className,
  iconSize = 12,
}: FavoriteButtonProps) => {
  const { workspaceId } = useWorkspace();
  const [toggleFavorite, { isLoading }] = useToggleFavoriteMutation();

  // Read isFavorite directly from the entity's own record in the store
  const isFavorite = useSelector((state: RootState) => {
    if (entityLayerType === EntityLayerType.ProjectSpace)
      return !!spaceSelectors.selectById(state, entityId)?.isFavorite;
    if (entityLayerType === EntityLayerType.ProjectFolder)
      return !!folderSelectors.selectById(state, entityId)?.isFavorite;
    if (entityLayerType === EntityLayerType.ProjectTask)
      return !!taskSelectors.selectById(state, entityId)?.isFavorite;
    return false;
  });

  // While the toggle is in-flight, show the opposite state for instant feedback
  const displayed = isLoading ? !isFavorite : isFavorite;

  const handleToggle = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (!workspaceId || isLoading) return;
    toggleFavorite({ workspaceId, entityId, entityLayerType });
  };

  return (
    <button
      type="button"
      onClick={handleToggle}
      disabled={isLoading}
      title={displayed ? "Remove from favorites" : "Add to favorites"}
      className={cn(
        "flex items-center justify-center transition-colors",
        displayed
          ? "text-amber-400"
          : "text-muted-foreground/30 hover:text-amber-400 opacity-0 group-hover:opacity-100",
        className
      )}
    >
      <Star
        size={iconSize}
        className={cn("transition-all", displayed ? "fill-amber-400" : "fill-transparent")}
      />
    </button>
  );
};
