import { useState } from "react";
import { useCreateFolder } from "../../contents/hierarchy/hierarchy-api";
import { Button } from "@/components/ui/button";
import { useWorkspace } from "../../context/workspace-provider";
import { toast } from "sonner";
import { IconColorPicker, PrivacyToggle, AttributeButton } from "./form-elements";
import * as Icons from "lucide-react";

interface CreateFolderFormProps {
  spaceId: string;
  onSuccess?: (id: string) => void;
  onCancel?: () => void;
}

export function CreateFolderForm({
  spaceId,
  onSuccess,
  onCancel,
}: CreateFolderFormProps) {
  const { workspaceId } = useWorkspace();
  const createFolder = useCreateFolder(workspaceId);
  const [name, setName] = useState("");
  const [isPrivate, setIsPrivate] = useState(false);
  const [icon, setIcon] = useState("Folder");
  const [color, setColor] = useState("#6366f1");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;

    try {
      const result = await createFolder.mutateAsync({
        spaceId,
        name,
        isPrivate,
        color,
        icon,
      });
      toast.success("Folder created");
      onSuccess?.((result as any).data);
    } catch (error) {
      toast.error("Failed to create folder");
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col w-full">
      {/* Main Header / Input Section */}
      <div className="px-3 pt-4 pb-2">
        <div className="flex items-center gap-3">
          <IconColorPicker 
            icon={icon} 
            color={color} 
            onChange={(i, c) => { setIcon(i); setColor(c); }} 
          />
          <input
            placeholder="Folder name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="flex-1 bg-transparent border-none focus:ring-0 text-[13px] font-semibold placeholder:text-muted-foreground/30 py-0 outline-none tracking-tight"
            autoFocus
          />
        </div>
      </div>

      {/* Attribute Strip */}
      <div className="px-3 py-1.5 flex flex-wrap items-center gap-1.5 border-t border-border/5">
        <PrivacyToggle isPrivate={isPrivate} onChange={setIsPrivate} />
        
        <AttributeButton icon={Icons.Circle}>
          Status
        </AttributeButton>

        <AttributeButton icon={Icons.Calendar}>
          Schedule
        </AttributeButton>
      </div>

      {/* Footer */}
      <div className="px-3 py-1.5 bg-background flex items-center justify-end gap-2 border-t border-border/10">
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
          disabled={!name.trim() || createFolder.isPending}
          className="h-7 px-4 text-[10px] font-semibold bg-primary hover:bg-primary/90 text-primary-foreground shadow-sm rounded-md transition-all active:scale-95"
        >
          {createFolder.isPending ? "Creating..." : "Create Folder"}
        </Button>
      </div>
    </form>
  );
}
