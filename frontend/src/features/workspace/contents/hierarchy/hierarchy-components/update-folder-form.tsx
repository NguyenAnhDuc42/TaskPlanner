import { useState } from "react";
import { useUpdateFolder } from "../hierarchy-api";
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
  initialInheritStatus: boolean;
}

export function UpdateFolderForm({
  id,
  initialName,
  initialColor,
  initialIcon,
  initialIsPrivate,
  initialInheritStatus,
}: Props) {
  const [name, setName] = useState(initialName);
  const [color, setColor] = useState(initialColor);
  const [icon, setIcon] = useState(initialIcon);
  const [isPrivate, setIsPrivate] = useState(initialIsPrivate);
  const [inheritStatus, setInheritStatus] = useState(initialInheritStatus);

  const updateFolder = useUpdateFolder();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;

    try {
      await updateFolder.mutateAsync({
        folderId: id,
        name: name,
        color: color,
        icon: icon,
        isPrivate: isPrivate,
        inheritStatus: inheritStatus,
      });
      toast.success("Folder updated successfully");
    } catch (error: unknown) {
      console.error("Failed to update folder", error);
      const errorMessage =
        error instanceof Error ? error.message : "Failed to update folder";
      toast.error(errorMessage);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4 pt-2">
      <div className="space-y-4 max-h-[60vh] overflow-y-auto px-1">
        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="folder-name">Name</Label>
            <Input
              id="folder-name"
              placeholder="Enter folder name..."
              value={name}
              onChange={(e) => setName(e.target.value)}
              disabled={updateFolder.isPending}
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

          <div className="space-y-3">
            <div className="flex items-center space-x-2">
              <Checkbox
                id="isFolderPrivate"
                checked={isPrivate}
                onCheckedChange={(checked) => setIsPrivate(checked === true)}
              />
              <Label
                htmlFor="isFolderPrivate"
                className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
              >
                Private folder
              </Label>
            </div>

            <div className="flex items-center space-x-2 border-t pt-3">
              <Checkbox
                id="inheritStatus"
                checked={inheritStatus}
                onCheckedChange={(checked) =>
                  setInheritStatus(checked === true)
                }
              />
              <div className="grid gap-1.5 leading-none">
                <Label
                  htmlFor="inheritStatus"
                  className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
                >
                  Inherit status from Space
                </Label>
                <p className="text-xs text-muted-foreground">
                  If disabled, this folder will have its own independent
                  statuses.
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="flex justify-end gap-2 pt-4 border-t">
        <Button type="submit" disabled={updateFolder.isPending || !name.trim()}>
          {updateFolder.isPending && (
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          )}
          Save Changes
        </Button>
      </div>
    </form>
  );
}
