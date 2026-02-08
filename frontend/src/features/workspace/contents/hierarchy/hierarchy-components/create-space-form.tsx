import { useState } from "react";
import { useCreateSpace } from "../hierarchy-api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader2 } from "lucide-react";
import { ColorPicker } from "@/components/color-picker";
import IconPicker from "@/components/icon-picker";

interface Props {
  onSuccess?: () => void;
}

export function CreateSpaceForm({ onSuccess }: Props) {
  const [name, setName] = useState("");
  const [color, setColor] = useState("#808080");
  const [icon, setIcon] = useState("LayoutGrid"); // Default space icon name

  const createSpace = useCreateSpace();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;

    try {
      await createSpace.mutateAsync({
        name: name,
        color: color,
        icon: icon,
      });
      setName("");
      setColor("#808080");
      setIcon("LayoutGrid");
      onSuccess?.();
    } catch (error) {
      console.error("Failed to create space", error);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="space-name">Space Name</Label>
        <Input
          id="space-name"
          placeholder="Enter space name..."
          value={name}
          onChange={(e) => setName(e.target.value)}
          disabled={createSpace.isPending}
        />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label>Color</Label>
          <ColorPicker value={color} onChange={setColor} />
        </div>
        <div className="space-y-2">
          <Label>Icon</Label>
          <IconPicker value={icon} onChange={setIcon} />
        </div>
      </div>

      <div className="flex justify-end gap-2">
        <Button type="submit" disabled={createSpace.isPending || !name.trim()}>
          {createSpace.isPending && (
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          )}
          Create
        </Button>
      </div>
    </form>
  );
}
