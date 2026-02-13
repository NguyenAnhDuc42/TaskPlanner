import { useState } from "react";
import { useUpdateSpace } from "../hierarchy-api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader2, Users } from "lucide-react";
import { ColorPicker } from "@/components/color-picker";
import IconPicker from "@/components/icon-picker";
import { Checkbox } from "@/components/ui/checkbox";
import { toast } from "sonner";
import { MemberSelector } from "./member-selector";
import { useParams } from "@tanstack/react-router";
import { Separator } from "@/components/ui/separator";

interface Props {
  id: string;
  initialName: string;
  initialColor: string;
  initialIcon: string;
  initialIsPrivate: boolean;
  onSuccess?: () => void;
}

export function UpdateSpaceForm({
  id,
  initialName,
  initialColor,
  initialIcon,
  initialIsPrivate,
  onSuccess,
}: Props) {
  const { workspaceId } = useParams({ strict: false });
  const [name, setName] = useState(initialName);
  const [color, setColor] = useState(initialColor);
  const [icon, setIcon] = useState(initialIcon);
  const [isPrivate, setIsPrivate] = useState(initialIsPrivate);
  const [selectedMemberIds, setSelectedMemberIds] = useState<string[]>([]);

  const updateSpace = useUpdateSpace();

  const handleToggleMember = (memberId: string) => {
    setSelectedMemberIds((prev) =>
      prev.includes(memberId)
        ? prev.filter((id) => id !== memberId)
        : [...prev, memberId],
    );
  };

  const handleRemoveMember = (memberId: string) => {
    setSelectedMemberIds((prev) => prev.filter((id) => id !== memberId));
  };

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
        memberIdsToAdd: selectedMemberIds,
      });
      toast.success("Space updated successfully");
      onSuccess?.();
    } catch (error: any) {
      console.error("Failed to update space", error);
      toast.error(error.message || "Failed to update space");
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4 pt-2">
      <div className="space-y-4 max-h-[60vh] overflow-y-auto px-1">
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

        {isPrivate && (
          <div className="space-y-3 pt-2">
            <Separator />
            <div className="flex items-center gap-2 text-sm font-medium">
              <Users className="h-4 w-4" />
              <span>Share with members</span>
            </div>
            <MemberSelector
              workspaceId={workspaceId || ""}
              selectedIds={selectedMemberIds}
              onToggle={handleToggleMember}
              onRemove={handleRemoveMember}
            />
          </div>
        )}
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
