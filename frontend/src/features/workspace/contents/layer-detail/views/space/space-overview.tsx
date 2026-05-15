import * as Icons from "lucide-react";
import { DescriptionSection } from "../../components/overview/description-section";
import { UniversalPicker } from "@/components/universal-picker";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";


interface SpaceOverviewProps {
  viewData: any;
  draft: any;
  onChange: (updates: any) => void;
  rightPanelType: "properties" | "attachments" | null;
}

export function SpaceOverview({ viewData, draft, onChange, }: SpaceOverviewProps) {
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

        {/* --- PROPERTIES ROW --- */}
        <div className="flex items-center gap-1.5 flex-wrap mt-2">
          {/* Visibility */}
          <div 
            onClick={() => onChange({ isPrivate: !draft?.isPrivate })}
            className="flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-muted/50 border border-border/10 text-[10px] font-bold text-muted-foreground hover:bg-muted transition-colors cursor-pointer"
          >
            <Icons.Lock className="h-3 w-3 stroke-[2.5px]" />
            <span>{draft?.isPrivate ? "Private" : "Public"}</span>
          </div>

          {/* Members */}
          <Popover>
            <PopoverTrigger asChild>
              <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-muted/50 border border-border/10 text-[10px] font-bold text-muted-foreground hover:bg-muted transition-colors cursor-pointer">
                <Icons.Users className="h-3 w-3 stroke-[2.5px]" />
                <span>{viewData.members?.length > 0 ? `${viewData.members.length} Users` : "No Members"}</span>
              </div>
            </PopoverTrigger>
            <PopoverContent className="w-48 p-2 bg-background/95 border-border/40 shadow-2xl rounded-xl" align="start">
              <div className="text-[10px] font-black uppercase tracking-wider text-muted-foreground/40 mb-2 px-1">Members</div>
              <div className="space-y-1">
                {viewData.members?.map((m: any, i: number) => (
                  <div key={i} className="flex items-center gap-2 p-1 hover:bg-muted/50 rounded-md transition-colors">
                    <div className="h-5 w-5 rounded-full bg-muted flex items-center justify-center text-[8px] font-bold uppercase overflow-hidden">
                      {m.avatarUrl ? <img src={m.avatarUrl} alt={m.name} className="h-full w-full object-cover" /> : <span>{m.name?.[0] || "?"}</span>}
                    </div>
                    <span className="text-[10px] font-bold">{m.name}</span>
                  </div>
                ))}
              </div>
            </PopoverContent>
          </Popover>
        </div>

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
