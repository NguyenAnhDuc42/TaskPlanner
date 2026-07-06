import React, { useMemo, useState } from "react";
import { observer } from "mobx-react-lite";
import { Star } from "lucide-react";
import { cn } from "@/lib/utils";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { FavoriteMutations } from "@/mutations/favorite.mutations";

interface FavoriteButtonProps {
  entityId: string;
  entityLayerType: EntityLayerType;
  className?: string;
  iconSize?: number;
}

export const FavoriteButton = observer(function FavoriteButton({
  entityId,
  entityLayerType,
  className,
  iconSize = 12,
}: FavoriteButtonProps) {
  const rootStore = useWorkspaceRootStore();
  const favoriteMutations = useMemo(() => new FavoriteMutations(rootStore), [rootStore]);
  const [isLoading, setIsLoading] = useState(false);


  const displayed = rootStore.favoriteStore.isFavorite(entityId);

  const handleToggle = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (isLoading) return;
    setIsLoading(true);
    favoriteMutations
      .toggle(entityId, entityLayerType)
      .catch((err) => console.error("Failed to toggle favorite", err))
      .finally(() => setIsLoading(false));
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
});
