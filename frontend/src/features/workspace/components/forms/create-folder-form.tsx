import { useState } from "react";
import { useCreateFolder } from "../../contents/hierarchy/hierarchy-api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useWorkspace } from "../../context/workspace-provider";
import { toast } from "sonner";
import { IconColorPicker, PrivacyToggle } from "./form-elements";

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
    <form onSubmit={handleSubmit} className="p-4 space-y-4 max-w-sm">
      <div className="flex items-center gap-2">
        <IconColorPicker 
          icon={icon} 
          color={color} 
          onChange={(i, c) => { setIcon(i); setColor(c); }} 
        />
        <Input
          placeholder="Folder Name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          className="h-8 bg-muted/20 border-border/50 focus-visible:ring-primary/40 text-xs font-bold"
          autoFocus
        />
      </div>

      <div className="flex items-center justify-between">
        <PrivacyToggle isPrivate={isPrivate} onChange={setIsPrivate} />
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
          disabled={!name.trim() || createFolder.isPending}
          className="h-8 px-4 text-[9px] font-black uppercase tracking-widest rounded-md"
        >
          {createFolder.isPending ? "..." : "Create"}
        </Button>
      </div>
    </form>
  );
}
