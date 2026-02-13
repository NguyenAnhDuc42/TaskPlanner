import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { Edit2, Trash2, Settings, Loader2 } from "lucide-react";
import {
  useDeleteSpace,
  useDeleteFolder,
  useDeleteList,
} from "../hierarchy-api";
import { useState } from "react";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { UpdateSpaceForm } from "./update-space-form";
import { UpdateFolderForm } from "./update-folder-form";
import { UpdateListForm } from "./update-list-form";
import { toast } from "sonner";

interface Props {
  type: "Space" | "Folder" | "List";
  id: string;
  name: string;
  color: string;
  icon: string;
  isPrivate: boolean;
}

export function ItemSettingPopover({
  type,
  id,
  name,
  color,
  icon,
  isPrivate,
}: Props) {
  const deleteSpace = useDeleteSpace();
  const deleteFolder = useDeleteFolder();
  const deleteList = useDeleteList();
  const [isDeleting, setIsDeleting] = useState(false);
  const [isEditOpen, setIsEditOpen] = useState(false);

  const handleDelete = async () => {
    if (!confirm(`Are you sure you want to delete ${type} "${name}"?`)) return;

    setIsDeleting(true);
    try {
      if (type === "Space") await deleteSpace.mutateAsync(id);
      if (type === "Folder") await deleteFolder.mutateAsync(id);
      if (type === "List") await deleteList.mutateAsync(id);
      toast.success(`${type} deleted successfully`);
    } catch (error: any) {
      console.error(`Failed to delete ${type}`, error);
      toast.error(
        error.message ||
          `Failed to delete ${type}. It may contain child items.`,
      );
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div className="flex flex-col gap-1 text-sm">
      <div className="font-medium px-2 py-1 text-xs text-muted-foreground uppercase tracking-wider">
        {type} Settings
      </div>
      <Separator />

      <DialogFormWrapper
        title={`Edit ${type}`}
        open={isEditOpen}
        onOpenChange={setIsEditOpen}
        trigger={
          <Button
            variant="ghost"
            size="sm"
            className="justify-start gap-2 px-2 h-8 font-normal"
          >
            <Edit2 className="h-4 w-4" />
            Rename / Edit
          </Button>
        }
      >
        {type === "Space" && (
          <UpdateSpaceForm
            id={id}
            initialName={name}
            initialColor={color}
            initialIcon={icon}
            initialIsPrivate={isPrivate}
            onSuccess={() => setIsEditOpen(false)}
          />
        )}
        {type === "Folder" && (
          <UpdateFolderForm
            id={id}
            initialName={name}
            initialColor={color}
            initialIcon={icon}
            initialIsPrivate={isPrivate}
            onSuccess={() => setIsEditOpen(false)}
          />
        )}
        {type === "List" && (
          <UpdateListForm
            id={id}
            initialName={name}
            initialColor={color}
            initialIcon={icon}
            initialIsPrivate={isPrivate}
            onSuccess={() => setIsEditOpen(false)}
          />
        )}
      </DialogFormWrapper>

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
        disabled={isDeleting}
        className="justify-start gap-2 px-2 h-8 font-normal text-destructive hover:text-destructive hover:bg-destructive/10"
        onClick={handleDelete}
      >
        {isDeleting ? (
          <Loader2 className="h-4 w-4 animate-spin" />
        ) : (
          <Trash2 className="h-4 w-4" />
        )}
        Delete
      </Button>
    </div>
  );
}
