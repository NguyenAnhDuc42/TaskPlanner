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
import { StatusSelect } from "@/components/status-select";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Calendar } from "@/components/ui/calendar";
import { Globe, Lock, CheckCircle2 } from "lucide-react";
import { CreateStatusForm } from "@/features/workspace/components/forms/create-status-form";

import { useFolderEditor } from "./folder-editor-context";

export function FolderSidebar() {
  const { registry } = useWorkspace();
  const { folder, updateField } = useFolderEditor();
  const [collapsed, setCollapsed] = useState({
    properties: false,
  });
  const [isStatusModalOpen, setIsStatusModalOpen] = useState(false);
  
  if (!folder) return null;

  // For Workflow Section -> Use Folder Workflow
  const folderWorkflow = useMemo(() => {
    if (folder.workflowId) {
      return registry.workflows.find((w: any) => 
        w.id?.toLowerCase() === folder.workflowId?.toLowerCase()
      );
    }
    return null;
  }, [folder.workflowId, registry.workflows]);

  // Resolve current status from folder
  const currentStatus = folder.statusId ? registry.statusMap[folder.statusId] : folder.status;

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
                    onClick={() => updateField({ isPrivate: false })}
                    className={cn(
                      "flex-1 flex items-center justify-center gap-2 h-6 rounded-[4px] text-[10px] font-bold uppercase tracking-wider transition-all",
                      !folder.isPrivate ? "bg-background shadow-sm text-foreground ring-1 ring-border/10" : "text-muted-foreground hover:text-foreground"
                    )}
                  >
                    <Globe className="h-3 w-3" />
                    Public
                  </button>
                  <button
                    onClick={() => updateField({ isPrivate: true })}
                    className={cn(
                      "flex-1 flex items-center justify-center gap-2 h-6 rounded-[4px] text-[10px] font-bold uppercase tracking-wider transition-all",
                      folder.isPrivate ? "bg-background shadow-sm text-foreground ring-1 ring-border/10" : "text-muted-foreground hover:text-foreground"
                    )}
                  >
                    <Lock className="h-3 w-3" />
                    Private
                  </button>
              </div>
            </div>

            <StatusSelect
              value={folder.statusId}
              onChange={(statusId) => updateField({ statusId })}
              workflowId={folder.parentWorkflowId}
              align="end"
              trigger={
                <div className="flex items-center justify-between py-1.5 px-1 hover:bg-muted/30 rounded-lg transition-all group cursor-pointer border border-transparent hover:border-border/10">
                    <div className="flex items-center gap-2.5 text-muted-foreground/40 group-hover:text-muted-foreground/80">
                      <Layers className="h-3.5 w-3.5" />
                      <span className="text-[10px] font-bold uppercase tracking-wider">Status</span>
                    </div>
                    <StatusBadge status={currentStatus} />
                </div>
              }
            />

            {/* Subtle Divider */}
            <div className="border-b border-border/5 my-1" />

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
                          {folder.startDate ? format(new Date(folder.startDate), "MM/dd/yyyy") : "TBD"}
                        </span>
                      </button>
                    </PopoverTrigger>
                    <PopoverContent className="w-auto p-0" align="start">
                      <Calendar
                        mode="single"
                        selected={folder.startDate ? new Date(folder.startDate) : undefined}
                        onSelect={(date) => updateField({ startDate: date?.toISOString() })}
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
                          {folder.dueDate ? format(new Date(folder.dueDate), "MM/dd/yyyy") : "TBD"}
                        </span>
                      </button>
                    </PopoverTrigger>
                    <PopoverContent className="w-auto p-0" align="end">
                      <Calendar
                        mode="single"
                        selected={folder.dueDate ? new Date(folder.dueDate) : undefined}
                        onSelect={(date) => updateField({ dueDate: date?.toISOString() })}
                        initialFocus
                      />
                    </PopoverContent>
                  </Popover>
                </div>
            </div>

            {/* Stronger Divider for Workflow */}
            <div className="border-b border-border/10 my-2" />

            {/* 4. Workflow */}
            <div 
              className="flex flex-col gap-2 py-2.5 px-3 bg-muted/10 rounded-lg border border-border/5 hover:border-border/10 transition-all cursor-pointer"
              onClick={() => setIsStatusModalOpen(true)}
            >
              <div className="flex items-center justify-between w-full text-muted-foreground/40 hover:text-muted-foreground/80 transition-colors">
                <div className="flex items-center gap-2.5">
                  <CheckCircle2 className="h-3.5 w-3.5" />
                  <span className="text-[10px] font-black uppercase tracking-wider">Workflow</span>
                </div>
                <span className="text-[10px] font-bold text-foreground/80">{(folderWorkflow?.statuses || []).length} Statuses</span>
              </div>
            </div>
          </div>
        )}
      </section>

      {folderWorkflow && (
        <CreateStatusForm
          isOpen={isStatusModalOpen}
          onClose={() => setIsStatusModalOpen(false)}
          workflowId={folderWorkflow.id}
        />
      )}
    </div>
  );
}
