import * as Icons from "lucide-react";
import { EntityLayerType } from "@/types/entity-layer-type";
import { DescriptionSection } from "../components/overview/description-section";
import { useState, useCallback } from "react";
import { useUpdateSpace, useUpdateFolder, useUpdateTask } from "../layer-api";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { UniversalPicker } from "@/components/universal-picker";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";

interface OverviewViewProps {
  viewData: any;
  layerType: EntityLayerType;
}

export function OverviewView({ viewData, layerType }: OverviewViewProps) {
  const { workspaceId } = useWorkspace();
  const [localName, setLocalName] = useState(viewData?.name || "");

  const updateSpace = useUpdateSpace(workspaceId);
  const updateFolder = useUpdateFolder(workspaceId);
  const updateTask = useUpdateTask(workspaceId);

  const handleUpdate = useCallback(
    (updates: any) => {
      if (!viewData?.id) return;
      if (layerType === EntityLayerType.ProjectSpace)
        updateSpace.mutate({ spaceId: viewData.id, ...updates });
      if (layerType === EntityLayerType.ProjectFolder)
        updateFolder.mutate({ folderId: viewData.id, ...updates });
      if (layerType === EntityLayerType.ProjectTask)
        updateTask.mutate({ taskId: viewData.id, ...updates });
    },
    [layerType, viewData?.id, updateSpace, updateFolder, updateTask],
  );

  const onNameSubmit = () => {
    const trimmedName = localName.trim();
    if (trimmedName && trimmedName !== (viewData?.name || "")) {
      handleUpdate({ name: trimmedName });
    }
  };

  if (!viewData) return null;

  const IconComponent =
    (Icons as any)[viewData.icon || "Folder"] || Icons.LayoutGrid;
  const entityColor = viewData.color || "var(--primary)";

  return (
    <div className="h-full overflow-y-auto no-scrollbar bg-background selection:bg-primary/20">
      <div className="max-w-4xl mx-auto w-full pt-16 px-12 space-y-12 pb-32 animate-in fade-in duration-700">
        
        {/* --- IDENTITY HEADER --- */}
        <header className="flex items-center gap-6">
          <Popover>
            <PopoverTrigger asChild>
              <button
                className="h-10 w-10 rounded-lg flex items-center justify-center border border-border/10 flex-shrink-0 transition-all hover:bg-muted/50 shadow-sm"
                style={{
                  backgroundColor: `${entityColor}15`,
                  color: entityColor,
                }}
              >
                <IconComponent className="h-5 w-5 stroke-[2.5px]" />
              </button>
            </PopoverTrigger>
            <PopoverContent
              className="w-auto p-0 border-none bg-transparent shadow-none"
              sideOffset={12}
              align="start"
            >
              <UniversalPicker
                selectedIcon={viewData.icon || "Folder"}
                selectedColor={viewData.color || "#94a3b8"}
                onSelect={(icon, color) => handleUpdate({ icon, color })}
              />
            </PopoverContent>
          </Popover>

          <input
            value={localName}
            onChange={(e) => setLocalName(e.target.value)}
            onBlur={onNameSubmit}
            onKeyDown={(e) => e.key === "Enter" && onNameSubmit()}
            className="flex-1 bg-transparent border-none outline-none text-4xl font-black tracking-tight text-foreground placeholder:text-muted-foreground/10"
            placeholder="Untitled"
            spellCheck={false}
          />
        </header>

        {/* --- CONTENT AREA --- */}
        <div className="pl-1">
          <DescriptionSection
            initialValue={viewData.description || ""}
            onSave={(val) => handleUpdate({ description: val })}
          />
        </div>

      </div>
    </div>
  );
}
