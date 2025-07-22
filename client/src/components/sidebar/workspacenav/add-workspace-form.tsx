import * as React from "react";
import { Button } from "@/components/ui/button";
import { useCreateWorkspace } from "@/features/workspace/workspace-hooks";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Plus } from "lucide-react";

interface AddWorkspaceFormProps {
  onSuccess: () => void;
}

export function AddWorkspaceForm({ onSuccess }: AddWorkspaceFormProps) {
  const { mutate, isPending, isError, error, reset } = useCreateWorkspace();
  const [name, setName] = React.useState("");
  const [description, setDescription] = React.useState("");
  const [icon, setIcon] = React.useState("");
  const [color, setColor] = React.useState("");
  const [isPrivate, setIsPrivate] = React.useState(false);

  React.useEffect(() => {
    if (!isPending && !isError) {
      reset();
    }
  }, [isPending, isError, reset]);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    mutate(
      { name, description, icon, color, isPrivate },
      {
        onSuccess: () => {
          setName("");
          setDescription("");
          setIcon("");
          setColor("");
          setIsPrivate(false);
          onSuccess();
        },
      }
    );
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3">
      <input
        className="border rounded px-3 py-2"
        placeholder="Workspace name"
        value={name}
        onChange={(e) => setName(e.target.value)}
        required
      />
      <textarea
        className="border rounded px-3 py-2"
        placeholder="Description"
        value={description}
        onChange={(e) => setDescription(e.target.value)}
      />
      <input
        className="border rounded px-3 py-2"
        placeholder="Icon URL (optional)"
        value={icon}
        onChange={(e) => setIcon(e.target.value)}
      />
      <input
        className="border rounded px-3 py-2"
        placeholder="Color (optional)"
        value={color}
        onChange={(e) => setColor(e.target.value)}
      />
      <label className="flex items-center gap-2">
        <input
          type="checkbox"
          checked={isPrivate}
          onChange={(e) => setIsPrivate(e.target.checked)}
          className="size-4"
        />
        Private workspace
      </label>
      <Button type="submit" disabled={isPending} className="mt-2">
        {isPending ? "Creating..." : "Create Workspace"}
      </Button>
      {isError && (
        <div className="text-destructive text-sm mt-2">
          {error?.detail || "Failed to create workspace. Please try again."}
        </div>
      )}
    </form>
  );
}

export function AddWorkspaceButton({ 
  afterAdd
}: {
  afterAdd?: () => void;
}) {
  const [isOpen, setIsOpen] = React.useState(false);
  
  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogTrigger asChild>
        <Button 
          variant="outline" 
          className="w-full justify-start mt-2"
        >
          <Plus className="mr-2 size-4" />
          Add workspace
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Add Workspace</DialogTitle>
        </DialogHeader>
        <AddWorkspaceForm 
          onSuccess={() => {
            setIsOpen(false);
            if (afterAdd) afterAdd();
          }} 
        />
      </DialogContent>
    </Dialog>
  );
}