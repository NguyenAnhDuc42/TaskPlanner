import { 
  Layers, 
  Calendar as CalendarIcon, 
  ArrowRight,
  ChevronDown
} from "lucide-react";
import { format } from "date-fns";
import { cn } from "@/lib/utils";
import { useMemo, useState } from "react";
import { StatusBadge } from "@/components/status-badge";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Calendar } from "@/components/ui/calendar";
import { Globe, Lock, CheckCircle2, Plus } from "lucide-react";

import type {  EnrichedFolderDetailDto } from "./folder-types";

interface FolderSidebarProps {
  viewData: EnrichedFolderDetailDto;
  draft: any;
  onChange: (updates: any) => void;
}

export function FolderSidebar({ viewData, draft, onChange }: FolderSidebarProps) {
  const { registry } = useWorkspace();
  const [collapsed, setCollapsed] = useState({
    properties: false,
  });
  const [workflowExpanded, setWorkflowExpanded] = useState(false);
  
  if (!viewData) return null;

  // 1. For Status Selection -> Use Space Workflow
  const spaceWorkflow = useMemo(() => {
    if (viewData.parentWorkflowId) {
      return registry.workflows.find((w: any) => 
        w.id?.toLowerCase() === viewData.parentWorkflowId?.toLowerCase()
      );
    }
    return null;
  }, [viewData.parentWorkflowId, registry.workflows]);

  // 2. For Workflow Section -> Use Folder Workflow
  const folderWorkflow = useMemo(() => {
    if (viewData.workflowId) {
      return registry.workflows.find((w: any) => 
        w.id?.toLowerCase() === viewData.workflowId?.toLowerCase()
      );
    }
    return null;
  }, [viewData.workflowId, registry.workflows]);

  const statuses = spaceWorkflow?.statuses || [];

  // Group folder statuses by category
  const folderStatusesByCategory = useMemo(() => {
    const grouped: Record<string, any[]> = {};
    (folderWorkflow?.statuses || []).forEach((status: any) => {
      const cat = status.category || "Other";
      if (!grouped[cat]) grouped[cat] = [];
      grouped[cat].push(status);
    });
    return grouped;
  }, [folderWorkflow]);
  
  // Resolve current status from draft
  const currentStatus = useMemo(() => {
    return registry.statusMap[draft?.statusId] || viewData.status;
  }, [draft?.statusId, viewData.status, registry.statusMap]);

  const toggleCollapse = (key: keyof typeof collapsed) => {
    setCollapsed(prev => ({ ...prev, [key]: !prev[key] }));
  };

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
          <div className="space-y-1.5 animate-in slide-in-from-top-2 duration-300">
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

            {/* 2. Status Picker */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <div className="flex items-center justify-between py-1.5 px-1 hover:bg-muted/30 rounded-lg transition-all group cursor-pointer border border-transparent hover:border-border/10">
                    <div className="flex items-center gap-2.5 text-muted-foreground/40 group-hover:text-muted-foreground/80">
                      <Layers className="h-3.5 w-3.5" />
                      <span className="text-[10px] font-bold uppercase tracking-wider">Status</span>
                    </div>
                    <StatusBadge status={currentStatus} />
                </div>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-48 p-1 bg-background/95 backdrop-blur-md border-border/40 shadow-2xl rounded-xl">
                <div className="px-2 py-1.5 pb-2 border-b border-border/10 mb-1">
                  <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/40">Select Status</span>
                </div>
                
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

            {/* 3. Schedule */}
            <div className="group flex flex-col gap-2 py-2 px-1 hover:bg-muted/30 rounded-lg transition-all border border-transparent hover:border-border/10">
                <div className="flex items-center gap-2.5 text-muted-foreground/40 group-hover:text-muted-foreground/80">
                  <CalendarIcon className="h-3.5 w-3.5" />
                  <span className="text-[10px] font-black uppercase tracking-wider">Schedule</span>
                </div>
                
                <div className="flex items-center justify-between w-full bg-muted/40 px-3 py-1.5 rounded-md border border-border/5 group-hover:bg-muted/60 transition-all">
                  {/* Start Date */}
                  <Popover>
                    <PopoverTrigger asChild>
                      <button className="flex flex-col text-left hover:opacity-70 transition-opacity">
                        <span className="text-[7px] font-black uppercase tracking-[0.2em] text-muted-foreground/30 mb-0">Start</span>
                        <span className="text-[10px] font-black text-foreground/80 font-mono tracking-tight">
                          {draft?.startDate ? format(new Date(draft.startDate), "MM/dd/yyyy") : "TBD"}
                        </span>
                      </button>
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
                  
                  <ArrowRight className="h-3 w-3 text-muted-foreground/20" />

                  {/* Due Date */}
                  <Popover>
                    <PopoverTrigger asChild>
                      <button className="flex flex-col items-end text-right hover:opacity-70 transition-opacity">
                        <span className="text-[7px] font-black uppercase tracking-[0.2em] text-muted-foreground/30 mb-0">Due</span>
                        <span className="text-[10px] font-black text-foreground/80 font-mono tracking-tight">
                          {draft?.dueDate ? format(new Date(draft.dueDate), "MM/dd/yyyy") : "TBD"}
                        </span>
                      </button>
                    </PopoverTrigger>
                    <PopoverContent className="w-auto p-0" align="end">
                      <Calendar
                        mode="single"
                        selected={draft?.dueDate ? new Date(draft.dueDate) : undefined}
                        onSelect={(date) => onChange({ dueDate: date?.toISOString() })}
                        initialFocus
                      />
                    </PopoverContent>
                  </Popover>
                </div>
            </div>

            {/* 4. Workflow */}
            <div className="flex flex-col gap-2 py-2.5 px-2 bg-muted/10 rounded-lg border border-border/5 hover:border-border/10 transition-all">
              <button 
                onClick={() => setWorkflowExpanded(!workflowExpanded)}
                className="flex items-center justify-between w-full text-muted-foreground/40 hover:text-muted-foreground/80 transition-colors"
              >
                <div className="flex items-center gap-2.5">
                  <CheckCircle2 className="h-3.5 w-3.5" />
                  <span className="text-[10px] font-black uppercase tracking-wider">Workflow</span>
                </div>
                <ChevronDown className={cn("h-3 w-3 transition-transform duration-200", workflowExpanded && "rotate-180")} />
              </button>
              
              {workflowExpanded && (
                <div className="space-y-3 pl-6 pt-1 animate-in slide-in-from-top-1 duration-200">
                  {folderWorkflow ? (
                    Object.entries(folderStatusesByCategory).map(([category, cats]) => (
                      <div key={category} className="space-y-1.5">
                        <div className="flex items-center justify-between">
                          <span className="text-[8px] font-black uppercase tracking-[0.1em] text-muted-foreground/50">{category}</span>
                        </div>
                        <div className="flex flex-wrap gap-1">
                          {cats.map((status: any) => (
                            <StatusBadge key={status.id} status={status} className="border-none bg-muted/40" />
                          ))}
                        </div>
                      </div>
                    ))
                  ) : (
                    <div className="text-[10px] text-muted-foreground/50 italic py-1">
                      No workflow found for this Folder.
                    </div>
                  )}

                  {/* Single button at the bottom */}
                  <button className="w-full mt-2 flex items-center justify-center gap-1.5 py-1.5 border border-dashed border-border/20 rounded-md hover:bg-muted/50 text-muted-foreground/60 hover:text-foreground transition-all">
                    <Plus className="h-3 w-3" />
                    <span className="text-[10px] font-black uppercase tracking-wider">Add Status</span>
                  </button>
                </div>
              )}
            </div>
          </div>
        )}
      </section>
    </div>
  );
}
