import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { Edit2, Trash2, Settings } from "lucide-react";

interface Props {
  type: "Space" | "Folder" | "List";
  id: string;
  name: string;
}

export function ItemSettingPopover({ type, id, name }: Props) {
  const handleDelete = () => {
    // TODO: Implement delete mutation
    console.log(`Delete ${type} ${id}`);
    alert(`Delete feature for ${type} "${name}" coming soon!`);
  };

  const handleRename = () => {
    // TODO: Implement rename dialog/mutation
    console.log(`Rename ${type} ${id}`);
    alert(`Rename feature for ${type} "${name}" coming soon!`);
  };

  return (
    <div className="flex flex-col gap-1 text-sm">
      <div className="font-medium px-2 py-1 text-xs text-muted-foreground uppercase tracking-wider">
        {type} Settings
      </div>
      <Separator />

      <Button
        variant="ghost"
        size="sm"
        className="justify-start gap-2 px-2 h-8 font-normal"
        onClick={handleRename}
      >
        <Edit2 className="h-4 w-4" />
        Rename
      </Button>

      <Button
        variant="ghost"
        size="sm"
        className="justify-start gap-2 px-2 h-8 font-normal"
        onClick={() => console.log("Settings")}
      >
        <Settings className="h-4 w-4" />
        {type} Settings
      </Button>

      <Separator />

      <Button
        variant="ghost"
        size="sm"
        className="justify-start gap-2 px-2 h-8 font-normal text-destructive hover:text-destructive hover:bg-destructive/10"
        onClick={handleDelete}
      >
        <Trash2 className="h-4 w-4" />
        Delete
      </Button>
    </div>
  );
}
