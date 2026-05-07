import { Loader2 } from "lucide-react";
import { ItemsDisplayer } from "../components/items/items-displayer";
import { EntityLayerType } from "@/types/entity-layer-type";
import type { ItemsViewMode, TaskViewData } from "../layer-detail-types";

interface ItemsViewProps {
  viewData: TaskViewData | any;
  isLoading: boolean;
  layerType: EntityLayerType;
  viewMode: ItemsViewMode;
}

export function ItemsView({ viewData, isLoading, viewMode }: ItemsViewProps) {
  if (isLoading) {
    return (
      <div className="h-full w-full flex items-center justify-center">
        <Loader2 className="h-5 w-5 animate-spin text-muted-foreground/20" />
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col overflow-hidden">
      {/* --- Content Area --- */}
      <div className="flex-1 overflow-hidden relative">
        {!viewData ? (
          <div className="h-full w-full flex items-center justify-center text-[10px] font-bold uppercase tracking-widest text-muted-foreground/20">
            No Items Found
          </div>
        ) : (
          <ItemsDisplayer 
            viewData={viewData} 
            viewMode={viewMode} 
          />
        )}
      </div>
    </div>
  );
}
