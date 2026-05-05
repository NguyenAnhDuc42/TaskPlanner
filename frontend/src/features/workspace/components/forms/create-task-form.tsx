import { useState } from "react";
import { useCreateTask } from "../../contents/hierarchy/hierarchy-api";
import { Button } from "@/components/ui/button";
import { useWorkspace } from "../../context/workspace-provider";
import { EntityLayerType } from "@/types/entity-layer-type";
import { Priority } from "@/types/priority";
import { Flag, Circle, Command, User } from "lucide-react";
import { toast } from "sonner";
import { AttributeButton, IconColorPicker, SimpleDatePicker } from "./form-elements";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
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
  const [icon, setIcon] = useState("Circle");
  const [color, setColor] = useState("#94a3b8");

  const onSubmit = async (e?: React.FormEvent) => {
    e?.preventDefault();
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
    <form onSubmit={onSubmit} className="flex flex-col w-full">
      {/* Main Header / Input Section */}
      <div className="px-3 pt-4 pb-2">
        <div className="flex items-start gap-3">
          <div className="mt-1">
            <IconColorPicker 
              icon={icon} 
              color={color} 
              onChange={(i, c) => { setIcon(i); setColor(c); }} 
            />
          </div>
          <textarea
            placeholder="Task title"
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="flex-1 bg-transparent border-none focus:ring-0 text-[13px] font-semibold placeholder:text-muted-foreground/30 py-0 outline-none resize-none min-h-[22px]"
            autoFocus
            rows={1}
            onKeyDown={(e) => {
              if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault();
                onSubmit();
              }
            }}
          />
        </div>
        
        <div className="pl-9">
          <textarea
            placeholder="Add description..."
            className="w-full bg-transparent border-none focus:ring-0 text-[12px] text-muted-foreground placeholder:text-muted-foreground/20 resize-none min-h-[18px] py-0 outline-none"
            rows={1}
          />
        </div>
      </div>

      {/* Attribute Strip */}
      <div className="px-3 py-1.5 flex flex-wrap items-center gap-1.5 border-t border-border/5">
        <AttributeButton icon={Circle}>
          Todo
        </AttributeButton>

        <Select value={priority} onValueChange={(v) => setPriority(v as Priority)}>
          <SelectTrigger asChild>
             <AttributeButton 
               icon={Flag} 
               className={cn(
                 priority === Priority.Urgent && "text-destructive hover:text-destructive",
                 priority === Priority.High && "text-orange-500 hover:text-orange-500",
                 priority === Priority.Normal && "text-primary hover:text-primary"
               )}
             >
               {priority}
             </AttributeButton>
          </SelectTrigger>
          <SelectContent className="border-border/50 shadow-xl rounded-lg">
            <SelectItem value={Priority.Low} className="text-xs">Low</SelectItem>
            <SelectItem value={Priority.Normal} className="text-xs">Normal</SelectItem>
            <SelectItem value={Priority.High} className="text-xs">High</SelectItem>
            <SelectItem value={Priority.Urgent} className="text-xs text-destructive">Urgent</SelectItem>
          </SelectContent>
        </Select>

        <SimpleDatePicker onChange={() => {}} />
        
        <AttributeButton icon={User} className="ml-auto">
          Assignee
        </AttributeButton>
      </div>

      {/* Footer Actions */}
      <div className="px-3 py-1.5 bg-background flex items-center justify-between border-t border-border/10">
        <div className="flex items-center gap-1">
           <div className="px-1.5 py-0.5 rounded border border-border/50 bg-muted/30 text-[9px] font-medium text-muted-foreground flex items-center gap-1">
             <Command className="h-2 w-2" /> Enter
           </div>
           <span className="text-[10px] text-muted-foreground/30 font-medium">to create</span>
        </div>

        <div className="flex items-center gap-2">
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={onCancel}
            className="h-7 px-2.5 text-[10px] font-medium text-muted-foreground hover:text-foreground"
          >
            Cancel
          </Button>
          <Button
            type="submit"
            size="sm"
            disabled={!name.trim() || createTask.isPending}
            className="h-7 px-4 text-[10px] font-semibold bg-primary hover:bg-primary/90 text-primary-foreground shadow-sm rounded-md transition-all active:scale-95"
          >
            {createTask.isPending ? "Creating..." : "Create Task"}
          </Button>
        </div>
      </div>
    </form>
  );
}
