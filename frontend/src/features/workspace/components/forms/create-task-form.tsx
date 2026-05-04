import { useState } from "react";
import { useCreateTask } from "../../contents/hierarchy/hierarchy-api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useWorkspace } from "../../context/workspace-provider";
import { EntityLayerType } from "@/types/entity-layer-type";
import { Priority } from "@/types/priority";
import { Flag, Circle } from "lucide-react";
import { toast } from "sonner";
import { IconColorPicker, SimpleDatePicker } from "./form-elements";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { cn } from "@/lib/utils";

interface CreateTaskFormProps {
  parentId: string;
  parentType: EntityLayerType;
  onSuccess?: (task: any) => void;
  onCancel?: () => void;
}

export function CreateTaskForm({
  parentId,
  parentType,
  onSuccess,
  onCancel,
}: CreateTaskFormProps) {
  const { workspaceId } = useWorkspace();
  const createTask = useCreateTask(workspaceId);
  const [name, setName] = useState("");
  const [priority, setPriority] = useState<Priority>(Priority.Normal);
  const [icon, setIcon] = useState("CheckCircle2");
  const [color, setColor] = useState("#94a3b8");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;

    try {
      const result = await createTask.mutateAsync({
        parentId,
        parentType,
        name,
        priority: priority as any,
        icon,
        color,
      });
      toast.success("Task created");
      onSuccess?.((result as any).data);
    } catch (error) {
      toast.error("Failed to create task");
    }
  };

  return (
    <form onSubmit={handleSubmit} className="p-4 space-y-3 max-w-sm">
      <div className="flex items-center gap-2">
        <IconColorPicker 
          icon={icon} 
          color={color} 
          onChange={(i, c) => { setIcon(i); setColor(c); }} 
        />
        <Input
          placeholder="Task Name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          className="h-8 bg-muted/20 border-border/50 focus-visible:ring-primary/40 text-xs font-bold"
          autoFocus
        />
      </div>

      <div className="flex flex-wrap items-center gap-2">
        {/* Status Holder (Empty for now) */}
        <Button 
          variant="ghost" 
          size="sm" 
          type="button"
          className="h-8 px-3 rounded-md bg-muted/30 hover:bg-muted/50 border border-border/50 text-[10px] font-bold uppercase tracking-tight text-muted-foreground"
        >
          <Circle className="h-3 w-3 mr-2" />
          Status
        </Button>

        {/* Priority */}
        <Select value={priority} onValueChange={(v) => setPriority(v as Priority)}>
          <SelectTrigger className="h-8 w-fit bg-muted/30 border-border/50 rounded-md text-[10px] font-bold uppercase tracking-tight">
            <div className="flex items-center gap-2 pr-1">
               <Flag className={cn("h-3 w-3", 
                  priority === Priority.Urgent ? "text-destructive" :
                  priority === Priority.High ? "text-orange-500" :
                  priority === Priority.Normal ? "text-primary" : "text-muted-foreground"
               )} />
               <SelectValue />
            </div>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={Priority.Low} className="text-[10px] font-bold uppercase">Low</SelectItem>
            <SelectItem value={Priority.Normal} className="text-[10px] font-bold uppercase">Normal</SelectItem>
            <SelectItem value={Priority.High} className="text-[10px] font-bold uppercase">High</SelectItem>
            <SelectItem value={Priority.Urgent} className="text-[10px] font-bold uppercase">Urgent</SelectItem>
          </SelectContent>
        </Select>

        {/* Date Picker */}
        <SimpleDatePicker onChange={() => {}} />
      </div>

      <div className="flex items-center justify-end gap-2 pt-2 border-t border-border/30">
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={onCancel}
          className="h-8 text-[9px] font-black uppercase tracking-widest opacity-50 hover:opacity-100"
        >
          Cancel
        </Button>
        <Button
          type="submit"
          size="sm"
          disabled={!name.trim() || createTask.isPending}
          className="h-8 px-4 text-[9px] font-black uppercase tracking-widest rounded-md"
        >
          {createTask.isPending ? "..." : "Create"}
        </Button>
      </div>
    </form>
  );
}
