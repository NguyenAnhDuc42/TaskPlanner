import * as Icons from "lucide-react";
import { DescriptionSection } from "../../components/overview/description-section";
import { UniversalPicker } from "@/components/universal-picker";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";

interface FolderOverviewProps {
  viewData: any;
  draft: any;
  onChange: (updates: any) => void;
}

export function FolderOverview({ viewData, draft, onChange }: FolderOverviewProps) {
  if (!viewData) return null;

  // Use optional chaining to prevent crash when draft is null on first load
  const IconComponent = (Icons as any)[draft?.icon || viewData?.icon] || Icons.LayoutGrid;
  const entityColor = draft?.color || viewData?.color;

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
                selectedIcon={draft?.icon || viewData?.icon}
                selectedColor={draft?.color || viewData?.color}
                onSelect={(icon, color) => onChange({ icon, color })}
              />
            </PopoverContent>
          </Popover>

          <input
            value={draft?.name ?? viewData?.name ?? ""}
            onChange={(e) => onChange({ name: e.target.value })}
            className="flex-1 bg-transparent border-none outline-none text-4xl font-black tracking-tight text-foreground placeholder:text-muted-foreground/10"
            placeholder="Untitled"
            spellCheck={false}
          />
        </header>

        {/* --- CONTENT AREA --- */}
        <div className="pl-1">
          <DescriptionSection
            documentId={viewData.defaultDocumentId}
          />
        </div>

      </div>
    </div>
  );
}
