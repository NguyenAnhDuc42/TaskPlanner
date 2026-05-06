import * as Icons from "lucide-react";
import { EntityLayerType } from "@/types/entity-layer-type";
import { DescriptionSection } from "../components/overview/description-section";
import { useState, useEffect, useCallback } from "react";
import { useDebounce } from "@/hooks/use-debounce";
import { 
  useUpdateSpace, 
  useUpdateFolder, 
  useUpdateTask 
} from "../../hierarchy/hierarchy-api";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { UniversalPicker } from "@/components/universal-picker";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";


interface OverviewViewProps {
  entityInfo: any;
  viewData: any;
  layerType: EntityLayerType;
}

export function OverviewView({ entityInfo, viewData, layerType }: OverviewViewProps) {
  const { workspaceId } = useWorkspace();
  const [localName, setLocalName] = useState(viewData?.name || "");
  const debouncedName = useDebounce(localName, 1000); // Increased debounce for safety

  const updateSpace = useUpdateSpace(workspaceId);
  const updateFolder = useUpdateFolder(workspaceId);
  const updateTask = useUpdateTask(workspaceId);

  // Sync from props only when ID changes to avoid loops during typing
  useEffect(() => {
    setLocalName(viewData?.name || "");
  }, [viewData?.id]);

  const handleUpdate = useCallback((updates: any) => {
    if (!viewData?.id) return;
    if (layerType === EntityLayerType.ProjectSpace) updateSpace.mutate({ spaceId: viewData.id, ...updates });
    if (layerType === EntityLayerType.ProjectFolder) updateFolder.mutate({ folderId: viewData.id, ...updates });
    if (layerType === EntityLayerType.ProjectTask) updateTask.mutate({ taskId: viewData.id, ...updates });
  }, [layerType, viewData?.id, updateSpace.mutate, updateFolder.mutate, updateTask.mutate]);

  useEffect(() => {
    if (debouncedName && debouncedName !== viewData?.name) {
      handleUpdate({ name: debouncedName });
    }
  }, [debouncedName, viewData?.name, handleUpdate]);

  if (!entityInfo || !viewData) return null;

  const IconComponent = (Icons as any)[viewData.icon || "Folder"] || Icons.LayoutGrid;
  const entityColor = viewData.color || "var(--primary)";

  return (
    <div className="h-full overflow-y-auto no-scrollbar bg-background selection:bg-primary/10">
      <div className="max-w-3xl mx-auto w-full pt-12 px-10 space-y-8 pb-20">
        
        {/* --- Main Document Area --- */}
        <div className="space-y-4">
          {/* Minimal Header */}
          <header className="flex items-center gap-4">
            <Popover>
              <PopoverTrigger asChild>
                <button 
                  className="h-12 w-12 rounded-xl flex items-center justify-center border border-border/10 flex-shrink-0 transition-all hover:scale-105 active:scale-95 shadow-lg group"
                  style={{ backgroundColor: `${entityColor}15`, color: entityColor }}
                >
                  <IconComponent className="h-6 w-6 stroke-[2.5px] transition-transform group-hover:rotate-12" />
                </button>
              </PopoverTrigger>
              <PopoverContent className="w-auto p-0 border-none bg-transparent shadow-none" sideOffset={12} align="start">
                <UniversalPicker 
                  selectedIcon={viewData.icon || "Folder"}
                  selectedColor={viewData.color || "#94a3b8"}
                  onSelect={(icon, color) => handleUpdate({ icon, color })}
                />
              </PopoverContent>
            </Popover>

            <div className="flex-1 min-w-0">
               <input
                 value={localName}
                 onChange={(e) => setLocalName(e.target.value)}
                 className="w-full bg-transparent border-none outline-none text-4xl font-black tracking-tight text-foreground placeholder:text-muted-foreground/20"
                 placeholder="Untitled Entity"
                 spellCheck={false}
               />
            </div>
          </header>

          {/* Description Content */}
          <div className="pt-2">
            <DescriptionSection 
              initialValue={viewData.description || ""} 
              onSave={(val) => handleUpdate({ description: val })}
            />
          </div>
        </div>

      </div>
    </div>
  );
}
