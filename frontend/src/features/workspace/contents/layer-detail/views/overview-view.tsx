import * as Icons from "lucide-react";
import { EntityLayerType } from "@/types/entity-layer-type";
import { DescriptionSection } from "../components/overview/description-section";
import { PropertySidebar } from "../components/overview/property-sidebar";
import { AttachmentSection } from "../components/overview/attachment-section";

interface OverviewViewProps {
  entityInfo: any;
  viewData: any;
  layerType: EntityLayerType;
}

export function OverviewView({ entityInfo, viewData, layerType }: OverviewViewProps) {
  if (!entityInfo || !viewData) return null;

  const IconComponent = (Icons as any)[entityInfo.icon] || Icons.LayoutGrid;
  const entityColor = entityInfo.color || "var(--primary)";

  return (
    <div className="h-full overflow-y-auto no-scrollbar bg-background selection:bg-primary/10">
      <div className="max-w-3xl mx-auto w-full pt-12 px-10 space-y-8 pb-20">
        
        {/* --- Main Document Area --- */}
        <div className="space-y-4">
          {/* Minimal Header */}
          <header className="flex items-center gap-4">
            <div 
              className="h-10 w-10 rounded-md flex items-center justify-center border border-border/10 flex-shrink-0"
              style={{ backgroundColor: `${entityColor}15`, color: entityColor }}
            >
              <IconComponent className="h-5 w-5 stroke-[2.5px]" />
            </div>
            <h1 className="text-3xl font-black tracking-tight text-foreground truncate">
              {entityInfo.name}
            </h1>
          </header>

          {/* Description Content */}
          <div className="pt-2">
            <DescriptionSection 
              initialValue={entityInfo.description} 
              onSave={(val) => console.log('Saving description:', val)}
            />
          </div>
        </div>

      </div>
    </div>
  );
}
