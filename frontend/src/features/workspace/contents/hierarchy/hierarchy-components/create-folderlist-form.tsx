import { useState } from "react";
import { useCreateFolder, useCreateList } from "../hierarchy-api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader2 } from "lucide-react";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { ColorPicker } from "@/components/color-picker";
import IconPicker from "@/components/icon-picker";

interface Props {
  workspaceId?: string;
  parentId: string;
  parentType: "Space" | "Folder";
  onSuccess?: () => void;
}

export function CreateFolderListForm({
  parentId,
  parentType,
  onSuccess,
}: Props) {
  // If parent is Folder, only "List" creation is allowed.
  // If parent is Space, allow "Folder" or "List".
  const [type, setType] = useState<"folder" | "list">(
    parentType === "Folder" ? "list" : "folder",
  );
  const [name, setName] = useState("");
  const [color, setColor] = useState("#808080");
  const [icon, setIcon] = useState(parentType === "Folder" ? "List" : "Folder");

  const createFolder = useCreateFolder();
  const createList = useCreateList();

  // Reset icon/color when type switches (optional, but good UX)
  const handleTypeChange = (val: "folder" | "list") => {
    setType(val);
    setIcon(val === "folder" ? "Folder" : "List");
  };

  const isSubmitting = createFolder.isPending || createList.isPending;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;

    try {
      if (type === "folder") {
        // Parent must be Space
        await createFolder.mutateAsync({
          spaceId: parentId,
          name: name,
          color: color,
          icon: icon,
        });
      } else {
        // Type is List
        if (parentType === "Space") {
          await createList.mutateAsync({
            spaceId: parentId,
            name: name,
            color: color,
            icon: icon,
          });
        } else {
          // Parent is Folder
          await createList.mutateAsync({
            folderId: parentId,
            name: name,
            color: color,
            icon: icon,
          });
        }
      }
      setName("");
      setColor("#808080");
      onSuccess?.();
    } catch (error) {
      console.error("Failed to create item", error);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {parentType === "Space" && (
        <RadioGroup
          value={type}
          onValueChange={(val) => handleTypeChange(val as "folder" | "list")}
          className="flex gap-4"
        >
          <div className="flex items-center space-x-2">
            <RadioGroupItem value="folder" id="r-folder" />
            <Label htmlFor="r-folder">Folder</Label>
          </div>
          <div className="flex items-center space-x-2">
            <RadioGroupItem value="list" id="r-list" />
            <Label htmlFor="r-list">List</Label>
          </div>
        </RadioGroup>
      )}

      <div className="space-y-2">
        <Label htmlFor="name">Name</Label>
        <Input
          id="name"
          placeholder={`Enter ${type} name...`}
          value={name}
          onChange={(e) => setName(e.target.value)}
          disabled={isSubmitting}
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
        <Button type="submit" disabled={isSubmitting || !name.trim()}>
          {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          Create
        </Button>
      </div>
    </form>
  );
}
