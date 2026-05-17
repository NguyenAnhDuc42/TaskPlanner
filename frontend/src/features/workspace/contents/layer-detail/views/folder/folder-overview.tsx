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
import { Calendar } from "@/components/ui/calendar";
import { CreateStatusForm } from "@/features/workspace/components/forms/create-status-form";
import { StatusSelect } from "@/components/status-select";

import { useEffect, useState, useMemo } from "react";
import { useFolderEditor } from "./folder-editor-context";

export function FolderOverview() {
  const { registry } = useWorkspace();
  const { folder, updateField } = useFolderEditor();
  const [localName, setLocalName] = useState("");
  const [isStatusModalOpen, setIsStatusModalOpen] = useState(false);

  useEffect(() => {
    if (folder) {
      setLocalName(folder.name || "");
    }
  }, [folder?.name]);

  const folderWorkflow = useMemo(() => {
    if (!folder) return null;
    if (folder.workflowId) {
      return registry.workflows.find((w: any) => 
        w.id?.toLowerCase() === folder.workflowId?.toLowerCase()
      );
    }
    return null;
  }, [folder?.workflowId, registry.workflows]);

  if (!folder) return null;

  const IconComponent = (Icons as any)[folder.icon || ""] || Icons.LayoutGrid;
  const entityColor = folder.color || "var(--primary)";

  const currentStatus = folder.statusId ? registry.statusMap[folder.statusId] : folder.status;

  const handleNameBlur = () => {
    if (localName.trim() !== folder.name) {
      updateField({ name: localName.trim() });
    }
  };

  const handleNameKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      e.currentTarget.blur();
    }
  };

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
                selectedIcon={folder.icon || ""}
                selectedColor={folder.color || ""}
                onSelect={(icon, color) => updateField({ icon, color })}
              />
            </PopoverContent>
          </Popover>

          <input
            value={localName}
            onChange={(e) => setLocalName(e.target.value)}
            onBlur={handleNameBlur}
            onKeyDown={handleNameKeyDown}
            className="flex-1 bg-transparent border-none outline-none text-4xl font-black tracking-tight text-foreground placeholder:text-muted-foreground/10"
            placeholder="Untitled"
            spellCheck={false}
          />
        </header>

        {/* --- PROPERTIES ROW --- */}
        <div className="flex items-center gap-1.5 flex-wrap mt-2">
          <StatusSelect
            value={folder.statusId}
            onChange={(statusId) => updateField({ statusId })}
            workflowId={folder.parentWorkflowId}
            align="start"
            trigger={
              <div className="cursor-pointer">
                <StatusBadge status={currentStatus} className="text-[10px] font-bold" />
              </div>
            }
          />

          {/* Start Date */}
          <Popover>
            <PopoverTrigger asChild>
              <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-muted/50 border border-border/10 text-[10px] font-bold text-muted-foreground hover:bg-muted transition-colors cursor-pointer">
                <Icons.Calendar className="h-3 w-3 stroke-[2.5px]" />
                <span>Start: {folder.startDate ? format(new Date(folder.startDate), "MMM dd") : "TBD"}</span>
              </div>
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

          {/* Due Date */}
          <Popover>
            <PopoverTrigger asChild>
              <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-muted/50 border border-border/10 text-[10px] font-bold text-muted-foreground hover:bg-muted transition-colors cursor-pointer">
                <Icons.Calendar className="h-3 w-3 stroke-[2.5px]" />
                <span>Due: {folder.dueDate ? format(new Date(folder.dueDate), "MMM dd") : "TBD"}</span>
              </div>
            </PopoverTrigger>
            <PopoverContent className="w-auto p-0" align="start">
              <Calendar
                mode="single"
                selected={folder.dueDate ? new Date(folder.dueDate) : undefined}
                onSelect={(date) => updateField({ dueDate: date?.toISOString() })}
                initialFocus
              />
            </PopoverContent>
          </Popover>

          {/* Workflow Statuses */}
          <div 
            onClick={() => setIsStatusModalOpen(true)}
            className="flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-muted/50 border border-border/10 text-[10px] font-bold text-muted-foreground hover:bg-muted transition-colors cursor-pointer animate-in fade-in duration-300"
          >
            <Icons.CheckCircle2 className="h-3 w-3 stroke-[2.5px]" />
            <span>Workflow Statuses</span>
          </div>
        </div>

        {/* --- CONTENT AREA --- */}
        {folder.defaultDocumentId && (
          <div className="pl-1">
            <DescriptionSection
              documentId={folder.defaultDocumentId}
            />
          </div>
        )}

      </div>

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
