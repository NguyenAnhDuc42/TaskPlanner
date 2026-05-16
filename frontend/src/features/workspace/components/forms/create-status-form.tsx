import { useEffect, useState } from "react";
import { Plus, GripVertical, MoreHorizontal, HelpCircle } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { StatusCategory } from "@/types/status-category";
import type { Status } from "@/types/status";


interface CreateStatusFormProps {
  isOpen: boolean;
  onClose: () => void;
  currentStatuses: Status[];
  onApplyChanges?: (statuses: Status[]) => void;
}

const PRESET_COLORS = [
  "#ef4444",
  "#f97316",
  "#f59e0b",
  "#10b981",
  "#06b6d4",
  "#3b82f6",
  "#6366f1",
  "#8b5cf6",
  "#ec4899",
  "#64748b",
];

export function CreateStatusForm({
  isOpen,
  onClose,
  currentStatuses,
  onApplyChanges,
}: CreateStatusFormProps) {
  const [localStatuses, setLocalStatuses] = useState<Status[]>(currentStatuses);
  const [name, setName] = useState("");
  const [addingToCategory, setAddingToCategory] = useState<StatusCategory | null>(null);

  useEffect(() => {
    setLocalStatuses(currentStatuses);
  }, [currentStatuses]);

  const groupedStatuses = {
    [StatusCategory.NotStarted]: localStatuses.filter(
      (s) => s.category === StatusCategory.NotStarted
    ),
    [StatusCategory.Active]: localStatuses.filter(
      (s) => s.category === StatusCategory.Active
    ),
    [StatusCategory.Done]: localStatuses.filter(
      (s) => s.category === StatusCategory.Done
    ),
    [StatusCategory.Closed]: localStatuses.filter(
      (s) => s.category === StatusCategory.Closed
    ),
  };


  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-sm bg-background border-border/40 text-foreground p-0 rounded-md">
        <DialogHeader className="border-b border-border/30 px-4 py-2.5">
          <DialogTitle className="text-base font-bold tracking-tight">
            Manage Statuses
          </DialogTitle>
        </DialogHeader>

        <div className="p-4 space-y-4 overflow-y-auto max-h-[60vh] [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/10 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/30 [&::-webkit-scrollbar-track]:bg-transparent">
          {Object.entries(groupedStatuses).map(([cat, statuses]) => (
            <div key={cat} className="space-y-2">
              {/* Category Header */}
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-1.5">
                  <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground/50">
                    {cat}
                  </span>
                  <HelpCircle className="h-3 w-3 text-muted-foreground/30" />
                </div>
                <button className="text-muted-foreground/40 hover:text-foreground">
                  <Plus className="h-3.5 w-3.5" />
                </button>
              </div>

              {/* Status List */}
              <div className="space-y-1.5">
                {statuses.map((s) => (
                  <div 
                    key={s.statusId} 
                    className="flex items-center gap-2 bg-muted/20 hover:bg-muted/30 px-2.5 h-8 rounded-md border border-border/40 group"
                  >
                    <GripVertical className="h-3 w-3 text-muted-foreground/30 cursor-move group-hover:text-muted-foreground/60" />
                    
                    {/* Color Dot (Clickable later) */}
                    <button 
                      className="h-2.5 w-2.5 rounded-full shrink-0 transition-transform hover:scale-110"
                      style={{ backgroundColor: s.color }}
                    />
                    
                    <span className="text-xs font-medium flex-1">{s.name}</span>
                    
                    <button className="text-muted-foreground/40 hover:text-foreground opacity-0 group-hover:opacity-100 transition-opacity">
                      <MoreHorizontal className="h-3 w-3" />
                    </button>
                  </div>
                ))}

                {/* Add Status Row */}
                {addingToCategory === cat ? (
                  <div className="flex items-center gap-2 bg-muted/20 px-2.5 h-8 rounded-md border border-border/40">
                    <div className="h-2 w-2 rounded-full shrink-0 bg-muted-foreground/30" />
                    <input 
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                      placeholder="Status name"
                      className="flex-1 h-6 bg-transparent p-0 text-xs focus:outline-none placeholder:text-muted-foreground/30"
                      autoFocus
                      onBlur={() => {
                        if (name.trim()) {
                          const newStatus: Status = {
                            statusId: `temp-${Date.now()}`,
                            name: name.trim(),
                            color: PRESET_COLORS[0],
                            category: cat as StatusCategory,
                          };
                          setLocalStatuses([...localStatuses, newStatus]);
                        }
                        setAddingToCategory(null);
                        setName("");
                      }}
                      onKeyDown={(e) => {
                        if (e.key === "Enter" && name.trim()) {
                          const newStatus: Status = {
                            statusId: `temp-${Date.now()}`,
                            name: name.trim(),
                            color: PRESET_COLORS[0],
                            category: cat as StatusCategory,
                          };
                          setLocalStatuses([...localStatuses, newStatus]);
                          setAddingToCategory(null);
                          setName("");
                        } else if (e.key === "Escape") {
                          setAddingToCategory(null);
                          setName("");
                        }
                      }}
                    />
                  </div>
                ) : (
                  <button 
                    className="w-full flex items-center gap-2 bg-transparent hover:bg-muted/10 px-2.5 h-8 rounded-md border border-dashed border-border/20 text-muted-foreground/40 hover:text-muted-foreground hover:border-border/40 transition-colors"
                    onClick={() => {
                      setAddingToCategory(cat as StatusCategory);
                      setName("");
                    }}
                  >
                    <Plus className="h-3 w-3 ml-1" />
                    <span className="text-xs font-medium">Add status</span>
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>

        {/* Footer */}
        <div className="border-t border-border/30 px-4 py-2.5 flex justify-end bg-muted/5">
          <Button 
            className="h-8 text-xs gap-1.5 rounded-md" 
            onClick={() => {
              onApplyChanges?.(localStatuses);
              onClose();
            }}
          >
            Apply changes
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
