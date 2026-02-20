import { useState } from "react";
import { useUpdateSpace } from "../hierarchy-api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader2 } from "lucide-react";
import { ColorPicker } from "@/components/color-picker";
import IconPicker from "@/components/icon-picker";
import { Checkbox } from "@/components/ui/checkbox";
import { toast } from "sonner";

interface Props {
  id: string;
  initialName: string;
  initialColor: string;
  initialIcon: string;
  initialIsPrivate: boolean;
}

export function UpdateSpaceForm({
  id,
  initialName,
  initialColor,
  initialIcon,
  initialIsPrivate,
}: Props) {
  const [name, setName] = useState(initialName);
  const [color, setColor] = useState(initialColor);
  const [icon, setIcon] = useState(initialIcon);
  const [isPrivate, setIsPrivate] = useState(initialIsPrivate);

  const updateSpace = useUpdateSpace();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;

    try {
      await updateSpace.mutateAsync({
        spaceId: id,
        name: name,
        color: color,
        icon: icon,
        isPrivate: isPrivate,
      });
      toast.success("Space updated successfully");
    } catch (error: unknown) {
      console.error("Failed to update space", error);
      const errorMessage =
        error instanceof Error ? error.message : "Failed to update space";
      toast.error(errorMessage);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4 pt-2">
      <div className="space-y-4 max-h-[60vh] overflow-y-auto px-1">
        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="space-name">Name</Label>
            <Input
              id="space-name"
              placeholder="Enter space name..."
              value={name}
              onChange={(e) => setName(e.target.value)}
              disabled={updateSpace.isPending}
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

          <div className="flex items-center space-x-2">
            <Checkbox
              id="isPrivate"
              checked={isPrivate}
              onCheckedChange={(checked) => setIsPrivate(checked === true)}
            />
            <Label
              htmlFor="isPrivate"
              className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
            >
              Private space
            </Label>
          </div>
        </div>
      </div>

      <div className="flex justify-end gap-2 pt-4 border-t">
        <Button type="submit" disabled={updateSpace.isPending || !name.trim()}>
          {updateSpace.isPending && (
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          )}
          Save Changes
        </Button>
      </div>
    </form>
  );
}
