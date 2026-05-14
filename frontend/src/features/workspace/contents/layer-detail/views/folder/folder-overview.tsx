import * as Icons from "lucide-react";
import { DescriptionSection } from "../../components/overview/description-section";
import { UniversalPicker } from "@/components/universal-picker";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { StatusBadge } from "@/components/status-badge";
import { format } from "date-fns";
import { useMemo } from "react";
import { Calendar } from "@/components/ui/calendar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

interface FolderOverviewProps {
  viewData: any;
  draft: any;
  onChange: (updates: any) => void;
  rightPanelType: "properties" | "attachments" | null;
}

export function FolderOverview({ viewData, draft, onChange, rightPanelType }: FolderOverviewProps) {
  const { registry } = useWorkspace();
  
  const statuses = useMemo(() => {
    if (viewData.parentWorkflowId) {
      const workflow = registry.workflows.find((w: any) => 
        w.id?.toLowerCase() === viewData.parentWorkflowId?.toLowerCase()
      );
      return workflow?.statuses || [];
    }
    return [];
  }, [viewData.parentWorkflowId, registry.workflows]);

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
          {/* Status */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <div className="cursor-pointer">
                <StatusBadge status={registry.statusMap[draft?.statusId] || viewData.status} className="text-[10px] font-bold" />
              </div>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start" className="w-48 p-1 bg-background/95 backdrop-blur-md border-border/40 shadow-2xl rounded-xl">
              {statuses.map((status: any) => (
                <DropdownMenuItem 
                  key={status.id}
                  onSelect={() => onChange({ statusId: status.id })}
                  className="p-1 rounded-lg cursor-pointer transition-colors"
                >
                  <StatusBadge status={status} className="w-full justify-start border-none bg-transparent hover:bg-muted/20" />
                </DropdownMenuItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>

          {/* Start Date */}
          <Popover>
            <PopoverTrigger asChild>
              <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-muted/50 border border-border/10 text-[10px] font-bold text-muted-foreground hover:bg-muted transition-colors cursor-pointer">
                <Icons.Calendar className="h-3 w-3 stroke-[2.5px]" />
                <span>Start: {draft?.startDate ? format(new Date(draft.startDate), "MMM dd") : "TBD"}</span>
              </div>
            </PopoverTrigger>
            <PopoverContent className="w-auto p-0" align="start">
              <Calendar
                mode="single"
                selected={draft?.startDate ? new Date(draft.startDate) : undefined}
                onSelect={(date) => onChange({ startDate: date?.toISOString() })}
                initialFocus
              />
            </PopoverContent>
          </Popover>

          {/* Due Date */}
          <Popover>
            <PopoverTrigger asChild>
              <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-muted/50 border border-border/10 text-[10px] font-bold text-muted-foreground hover:bg-muted transition-colors cursor-pointer">
                <Icons.Calendar className="h-3 w-3 stroke-[2.5px]" />
                <span>Due: {draft?.dueDate ? format(new Date(draft.dueDate), "MMM dd") : "TBD"}</span>
              </div>
            </PopoverTrigger>
            <PopoverContent className="w-auto p-0" align="start">
              <Calendar
                mode="single"
                selected={draft?.dueDate ? new Date(draft.dueDate) : undefined}
                onSelect={(date) => onChange({ dueDate: date?.toISOString() })}
                initialFocus
              />
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
