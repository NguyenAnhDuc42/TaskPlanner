import React from "react";
import { Star } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useToggleFavoriteMutation } from "@/features/workspace/api";
import { useWorkspace } from "@/features/workspace/context/workspace-context";

interface FavoriteButtonProps {
  entityId: string;
  entityLayerType: EntityLayerType;
  isFavorite: boolean;
  className?: string;
  iconSize?: number;
}

export const FavoriteButton = ({
  entityId,
  entityLayerType,
  isFavorite,
  className,
  iconSize = 16,
}: FavoriteButtonProps) => {
  const { workspaceId } = useWorkspace();
  const [toggleFavorite, { isLoading }] = useToggleFavoriteMutation();

  const handleToggle = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (!workspaceId) return;
    
    toggleFavorite({
      workspaceId,
      entityId,
      entityLayerType,
    });
  };

  return (
    <Button
      variant="ghost"
      size="icon"
      className={cn("h-8 w-8 text-muted-foreground hover:text-amber-400 transition-colors", className)}
      onClick={handleToggle}
      disabled={isLoading}
      title={isFavorite ? "Remove from Favorites" : "Add to Favorites"}
    >
      <Star
        size={iconSize}
        className={cn(
          "transition-all",
          isFavorite ? "fill-amber-400 text-amber-400" : "fill-transparent"
        )}
      />
    </Button>
  );
};
