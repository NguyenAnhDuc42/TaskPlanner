import { 
  Users, 
  Lock,
  Globe,
  ChevronDown,
  CheckCircle2,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useState, useMemo } from "react";
import type {  EnrichedSpaceDetailDto } from "./space-types";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { CreateStatusForm } from "@/features/workspace/components/forms/create-status-form";
import { useUpdateWorkflowStatuses } from "@/features/workspace/api";

interface SpaceSidebarProps {
  viewData: EnrichedSpaceDetailDto;
  draft: any;
  onChange: (updates: any) => void;
}

export function SpaceSidebar({ viewData, draft, onChange }: SpaceSidebarProps) {
  const { registry } = useWorkspace();

  const [collapsed, setCollapsed] = useState({
    properties: false,
  });

  const { mutate: updateStatuses } = useUpdateWorkflowStatuses();
  
  if (!viewData) return null;

  const toggleCollapse = (key: keyof typeof collapsed) => {
    setCollapsed(prev => ({ ...prev, [key]: !prev[key] }));
  };

  const workflow = useMemo(() => {
    if (viewData.workflowId) {
      return registry.workflows.find((w: any) => 
        w.id?.toLowerCase() === viewData.workflowId?.toLowerCase()
      );
    }
    return null;
  }, [viewData.workflowId, registry.workflows]);

  const statuses = workflow?.statuses || [];
  const [isStatusModalOpen, setIsStatusModalOpen] = useState(false);
  
  return (
    <div className="space-y-6">
      {/* --- Properties Section --- */}
      <section className="space-y-4">
        <button 
          onClick={() => toggleCollapse("properties")}
          className="w-full flex items-center justify-between group px-1"
        >
          <h3 className="text-[10px] font-black uppercase tracking-[0.2em] text-muted-foreground/30 group-hover:text-muted-foreground/60 transition-colors">Properties</h3>
          <div className={cn("transition-transform duration-300", collapsed.properties && "-rotate-90")}>
            <ChevronDown className="h-3 w-3 text-muted-foreground/20" />
          </div>
        </button>

        {!collapsed.properties && (
          <div className="space-y-2 animate-in slide-in-from-top-2 duration-300">
            {/* 1. Visibility */}
            <div className=" py-1">
              <div className="flex bg-muted/20 p-0.5 rounded-md border border-border/10 w-full">
                  <button
                    onClick={() => onChange({ isPrivate: false })}
                    className={cn(
                      "flex-1 flex items-center justify-center gap-2 h-6 rounded-[4px] text-[10px] font-bold uppercase tracking-wider transition-all",
                      !draft?.isPrivate ? "bg-background shadow-sm text-foreground ring-1 ring-border/10" : "text-muted-foreground hover:text-foreground"
                    )}
                  >
                    <Globe className="h-3 w-3" />
                    Public
                  </button>
                  <button
                    onClick={() => onChange({ isPrivate: true })}
                    className={cn(
                      "flex-1 flex items-center justify-center gap-2 h-6 rounded-[4px] text-[10px] font-bold uppercase tracking-wider transition-all",
                      draft?.isPrivate ? "bg-background shadow-sm text-foreground ring-1 ring-border/10" : "text-muted-foreground hover:text-foreground"
                    )}
                  >
                    <Lock className="h-3 w-3" />
                    Private
                  </button>
              </div>
            </div>

            {/* Subtle Divider */}
            <div className="border-b border-border/5 my-1" />

            {/* 2. Members */}
            <PropertyRow 
              icon={Users} 
              label="Members" 
              value={viewData.members?.length > 0 ? `${viewData.members.length} Users` : "No Members"} 
            />

            {/* Stronger Divider for Workflow */}
            <div className="border-b border-border/10 my-2" />

            <div 
              className="flex flex-col gap-2 py-2.5 px-3 bg-muted/10 rounded-lg border border-border/5 hover:border-border/10 transition-all cursor-pointer"
              onClick={() => setIsStatusModalOpen(true)}
            >
              <div className="flex items-center justify-between w-full text-muted-foreground/40 hover:text-muted-foreground/80 transition-colors">
                <div className="flex items-center gap-2.5">
                  <CheckCircle2 className="h-3.5 w-3.5" />
                  <span className="text-[10px] font-black uppercase tracking-wider">Workflow</span>
                </div>
                <span className="text-[10px] font-bold text-foreground/80">{statuses.length} Statuses</span>
              </div>
            </div>
          </div>
        )}
      </section>
      <CreateStatusForm
        isOpen={isStatusModalOpen}
        onClose={() => setIsStatusModalOpen(false)}
        currentStatuses={statuses.map((s: any) => ({
          statusId: s.statusId || s.id,
          name: s.name,
          color: s.color,
          category: s.category,
        }))}
        onApplyChanges={(newStatuses) => {
          if (!workflow?.id) return;
          
          updateStatuses({
            workflowId: workflow.id,
            statuses: newStatuses.map(s => ({
              id: s.statusId.startsWith("temp-") ? null : s.statusId,
              name: s.name,
              color: s.color,
              category: s.category
            }))
          });
        }}
      />
    </div>
  );
}

function PropertyRow({ 
  icon: Icon, 
  label, 
  value, 
  color 
}: { 
  icon: any, 
  label: string, 
  value: string, 
  color?: string
}) {
  return (
    <div className="flex items-center justify-between py-1.5 px-1 hover:bg-muted/30 rounded-lg transition-all group cursor-pointer border border-transparent hover:border-border/10">
      <div className="flex items-center gap-2.5 text-muted-foreground/40 group-hover:text-muted-foreground/80">
        <Icon className="h-3.5 w-3.5" />
        <span className="text-[10px] font-bold uppercase tracking-wider">{label}</span>
      </div>
      <div className="flex items-center gap-2">
        {color && <div className="h-1.5 w-1.5 rounded-[2px]" style={{ backgroundColor: color }} />}
        <span className="text-[10px] font-black text-foreground/80 tracking-tight transition-colors group-hover:text-foreground truncate max-w-[120px]">
          {value}
        </span>
      </div>
    </div>
  );
}
