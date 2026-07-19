import { useState } from "react";
import { ChevronDown } from "lucide-react";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import { cn } from "@/lib/utils";
import { FavoriteNodeList } from "../contents/hierarchy/items/favorite-node-list";
import { ProjectNodeList } from "../contents/hierarchy/items/project-node-list";

// Shared between desktop's AppSidebar and the mobile drawer so the two navs can't drift apart
// again the way the old per-platform hierarchy tree did.
export function FavoritesProjectsNav() {
  const [isFavoritesOpen, setIsFavoritesOpen] = useState(true);
  const [isProjectsOpen, setIsProjectsOpen] = useState(true);

  return (
    <div className="flex-1 min-h-0 flex flex-col">
      <div className="px-2 shrink-0">
        <Collapsible open={isFavoritesOpen} onOpenChange={setIsFavoritesOpen}>
          <CollapsibleTrigger className="flex items-center gap-1.5 w-full px-2 py-1 rounded-md hover:bg-muted/40 transition-colors">
            <ChevronDown className={cn("h-3 w-3 text-muted-foreground/50 transition-transform", !isFavoritesOpen && "-rotate-90")} />
            <span className="text-[9px] font-bold uppercase tracking-wider text-muted-foreground/40">Favorites</span>
          </CollapsibleTrigger>
          <CollapsibleContent className="px-0.5 pt-0.5 pb-1 max-h-40 overflow-y-auto [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
            <FavoriteNodeList />
          </CollapsibleContent>
        </Collapsible>
      </div>

      <div className="flex-1 min-h-0 flex flex-col px-2 pb-2">
        <Collapsible open={isProjectsOpen} onOpenChange={setIsProjectsOpen} className="flex-1 min-h-0 flex flex-col">
          <CollapsibleTrigger className="flex items-center gap-1.5 w-full px-2 py-1 rounded-md hover:bg-muted/40 transition-colors shrink-0">
            <ChevronDown className={cn("h-3 w-3 text-muted-foreground/50 transition-transform", !isProjectsOpen && "-rotate-90")} />
            <span className="text-[9px] font-bold uppercase tracking-wider text-muted-foreground/40">Projects</span>
          </CollapsibleTrigger>
          <CollapsibleContent className="flex-1 min-h-0 overflow-y-auto px-0.5 pt-0.5 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
            <ProjectNodeList />
          </CollapsibleContent>
        </Collapsible>
      </div>
    </div>
  );
}
